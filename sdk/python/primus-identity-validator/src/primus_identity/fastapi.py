"""
FastAPI integration for Primus Identity Validator.

Provides middleware and dependency injection for JWT validation.
"""

from __future__ import annotations

import logging
from functools import wraps
from typing import Any, Callable, List, Optional, Sequence, TypeVar

from primus_identity.types import PrimusUser, TokenValidationResult
from primus_identity.validator import PrimusIdentityValidator

logger = logging.getLogger("primus_identity.fastapi")

# FastAPI imports - optional dependency
try:
    from fastapi import Depends, HTTPException, Request, status
    from fastapi.security import HTTPAuthorizationCredentials, HTTPBearer
    from starlette.middleware.base import BaseHTTPMiddleware, RequestResponseEndpoint
    from starlette.responses import JSONResponse, Response

    FASTAPI_AVAILABLE = True
except ImportError:
    FASTAPI_AVAILABLE = False

    # Stub types for when FastAPI is not installed
    class Request:  # type: ignore
        pass

    class HTTPException(Exception):  # type: ignore
        pass

    class HTTPBearer:  # type: ignore
        pass


def _ensure_fastapi() -> None:
    """Ensure FastAPI is installed."""
    if not FASTAPI_AVAILABLE:
        raise ImportError(
            "FastAPI is not installed. Install it with: pip install primus-identity-validator[fastapi]"
        )


class PrimusIdentityMiddleware:
    """ASGI middleware for Primus Identity validation.

    This middleware validates JWT tokens in the Authorization header
    and attaches the PrimusUser to the request state.

    Example:
        >>> from fastapi import FastAPI
        >>> from primus_identity import PrimusIdentityValidator, PrimusIdentityOptions
        >>> from primus_identity.fastapi import PrimusIdentityMiddleware
        >>>
        >>> validator = PrimusIdentityValidator(options)
        >>> app = FastAPI()
        >>> app.add_middleware(PrimusIdentityMiddleware, validator=validator)
    """

    def __init__(
        self,
        app: Any,
        validator: PrimusIdentityValidator,
        exclude_paths: Optional[List[str]] = None,
        optional_paths: Optional[List[str]] = None,
    ) -> None:
        """Initialize the middleware.

        Args:
            app: The ASGI application.
            validator: The Primus Identity validator instance.
            exclude_paths: Paths to exclude from authentication (e.g., ["/health", "/docs"]).
            optional_paths: Paths where authentication is optional.
        """
        _ensure_fastapi()
        self.app = app
        self.validator = validator
        self.exclude_paths = exclude_paths or ["/health", "/docs", "/openapi.json", "/redoc"]
        self.optional_paths = optional_paths or []

    async def __call__(self, scope: Any, receive: Any, send: Any) -> None:
        """ASGI interface."""
        if scope["type"] != "http":
            await self.app(scope, receive, send)
            return

        from starlette.requests import Request as StarletteRequest

        request = StarletteRequest(scope, receive)
        path = request.url.path

        # Check if path is excluded
        if any(path.startswith(p) for p in self.exclude_paths):
            await self.app(scope, receive, send)
            return

        # Check for Authorization header
        auth_header = request.headers.get("Authorization")
        is_optional = any(path.startswith(p) for p in self.optional_paths)

        if not auth_header:
            if is_optional:
                scope["state"] = scope.get("state", {})
                scope["state"]["primus_user"] = None
                await self.app(scope, receive, send)
                return

            response = JSONResponse(
                status_code=401,
                content={"detail": "Missing Authorization header"},
            )
            await response(scope, receive, send)
            return

        # Extract Bearer token
        if not auth_header.startswith("Bearer "):
            response = JSONResponse(
                status_code=401,
                content={"detail": "Invalid Authorization header format. Expected 'Bearer <token>'"},
            )
            await response(scope, receive, send)
            return

        token = auth_header[7:]  # Remove "Bearer " prefix

        # Validate token
        result = await self.validator.validate_token(token)

        if not result.is_valid:
            if is_optional:
                scope["state"] = scope.get("state", {})
                scope["state"]["primus_user"] = None
                await self.app(scope, receive, send)
                return

            status_code = 401
            if result.error_code == "TOKEN_EXPIRED":
                status_code = 401
            elif result.error_code == "ISSUER_NOT_CONFIGURED":
                status_code = 403

            response = JSONResponse(
                status_code=status_code,
                content={
                    "detail": result.error,
                    "error_code": result.error_code,
                },
            )
            await response(scope, receive, send)
            return

        # Attach user to request state
        scope["state"] = scope.get("state", {})
        scope["state"]["primus_user"] = result.user
        scope["state"]["primus_validation_result"] = result

        await self.app(scope, receive, send)


def create_auth_dependency(
    validator: PrimusIdentityValidator,
    optional: bool = False,
) -> Callable[..., Any]:
    """Create a FastAPI dependency for token validation.

    Args:
        validator: The Primus Identity validator instance.
        optional: If True, authentication is optional.

    Returns:
        A FastAPI dependency function.

    Example:
        >>> from fastapi import FastAPI, Depends
        >>> from primus_identity.fastapi import create_auth_dependency
        >>>
        >>> get_current_user = create_auth_dependency(validator)
        >>>
        >>> @app.get("/me")
        >>> async def get_me(user: PrimusUser = Depends(get_current_user)):
        ...     return {"user_id": user.sub}
    """
    _ensure_fastapi()

    security = HTTPBearer(auto_error=not optional)

    async def dependency(
        credentials: Optional[HTTPAuthorizationCredentials] = Depends(security),
    ) -> Optional[PrimusUser]:
        if credentials is None:
            if optional:
                return None
            raise HTTPException(
                status_code=status.HTTP_401_UNAUTHORIZED,
                detail="Missing Authorization header",
            )

        result = await validator.validate_token(credentials.credentials)

        if not result.is_valid:
            if optional:
                return None

            status_code = status.HTTP_401_UNAUTHORIZED
            if result.error_code == "TOKEN_EXPIRED":
                status_code = status.HTTP_401_UNAUTHORIZED
            elif result.error_code == "ISSUER_NOT_CONFIGURED":
                status_code = status.HTTP_403_FORBIDDEN

            raise HTTPException(
                status_code=status_code,
                detail=result.error,
                headers={"WWW-Authenticate": "Bearer"},
            )

        return result.user

    return dependency


def require_roles(*roles: str) -> Callable[[Callable[..., Any]], Callable[..., Any]]:
    """Decorator to require specific roles.

    Args:
        *roles: Required roles (user must have at least one).

    Returns:
        A decorator function.

    Example:
        >>> @app.get("/admin")
        >>> @require_roles("admin", "superuser")
        >>> async def admin_endpoint(user: PrimusUser = Depends(get_current_user)):
        ...     return {"message": "Welcome, admin!"}
    """
    _ensure_fastapi()

    def decorator(func: Callable[..., Any]) -> Callable[..., Any]:
        @wraps(func)
        async def wrapper(*args: Any, **kwargs: Any) -> Any:
            # Find PrimusUser in kwargs
            user: Optional[PrimusUser] = None
            for value in kwargs.values():
                if isinstance(value, PrimusUser):
                    user = value
                    break

            if user is None:
                raise HTTPException(
                    status_code=status.HTTP_401_UNAUTHORIZED,
                    detail="Authentication required",
                )

            if not user.has_any_role(*roles):
                raise HTTPException(
                    status_code=status.HTTP_403_FORBIDDEN,
                    detail=f"Required roles: {', '.join(roles)}",
                )

            return await func(*args, **kwargs)

        return wrapper

    return decorator


def require_permissions(*permissions: str) -> Callable[[Callable[..., Any]], Callable[..., Any]]:
    """Decorator to require specific permissions.

    Args:
        *permissions: Required permissions (user must have all).

    Returns:
        A decorator function.
    """
    _ensure_fastapi()

    def decorator(func: Callable[..., Any]) -> Callable[..., Any]:
        @wraps(func)
        async def wrapper(*args: Any, **kwargs: Any) -> Any:
            user: Optional[PrimusUser] = None
            for value in kwargs.values():
                if isinstance(value, PrimusUser):
                    user = value
                    break

            if user is None:
                raise HTTPException(
                    status_code=status.HTTP_401_UNAUTHORIZED,
                    detail="Authentication required",
                )

            missing = [p for p in permissions if not user.has_permission(p)]
            if missing:
                raise HTTPException(
                    status_code=status.HTTP_403_FORBIDDEN,
                    detail=f"Missing permissions: {', '.join(missing)}",
                )

            return await func(*args, **kwargs)

        return wrapper

    return decorator


def require_scopes(*scopes: str) -> Callable[[Callable[..., Any]], Callable[..., Any]]:
    """Decorator to require specific OAuth scopes.

    Args:
        *scopes: Required scopes (token must have all).

    Returns:
        A decorator function.
    """
    _ensure_fastapi()

    def decorator(func: Callable[..., Any]) -> Callable[..., Any]:
        @wraps(func)
        async def wrapper(*args: Any, **kwargs: Any) -> Any:
            user: Optional[PrimusUser] = None
            for value in kwargs.values():
                if isinstance(value, PrimusUser):
                    user = value
                    break

            if user is None:
                raise HTTPException(
                    status_code=status.HTTP_401_UNAUTHORIZED,
                    detail="Authentication required",
                )

            missing = [s for s in scopes if not user.has_scope(s)]
            if missing:
                raise HTTPException(
                    status_code=status.HTTP_403_FORBIDDEN,
                    detail=f"Missing scopes: {', '.join(missing)}",
                )

            return await func(*args, **kwargs)

        return wrapper

    return decorator
