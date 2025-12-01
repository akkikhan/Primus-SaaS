"""
Flask integration for Primus Identity Validator.

Provides decorators and middleware for JWT validation.
"""

from __future__ import annotations

import asyncio
import logging
from functools import wraps
from typing import Any, Callable, List, Optional, Tuple

from primus_identity.types import PrimusUser, TokenValidationResult
from primus_identity.validator import PrimusIdentityValidator

logger = logging.getLogger("primus_identity.flask")

# Flask imports - optional dependency
try:
    from flask import Flask, Request, g, jsonify, request

    FLASK_AVAILABLE = True
except ImportError:
    FLASK_AVAILABLE = False

    class Flask:  # type: ignore
        pass

    class Request:  # type: ignore
        pass

    g = None  # type: ignore
    request = None  # type: ignore


def _ensure_flask() -> None:
    """Ensure Flask is installed."""
    if not FLASK_AVAILABLE:
        raise ImportError(
            "Flask is not installed. Install it with: pip install primus-identity-validator[flask]"
        )


def _run_async(coro: Any) -> Any:
    """Run an async coroutine in a sync context."""
    try:
        loop = asyncio.get_event_loop()
    except RuntimeError:
        loop = asyncio.new_event_loop()
        asyncio.set_event_loop(loop)

    return loop.run_until_complete(coro)


class PrimusIdentityExtension:
    """Flask extension for Primus Identity validation.

    Example:
        >>> from flask import Flask
        >>> from primus_identity import PrimusIdentityValidator, PrimusIdentityOptions
        >>> from primus_identity.flask import PrimusIdentityExtension
        >>>
        >>> app = Flask(__name__)
        >>> validator = PrimusIdentityValidator(options)
        >>> primus = PrimusIdentityExtension(app, validator)
    """

    def __init__(
        self,
        app: Optional[Flask] = None,
        validator: Optional[PrimusIdentityValidator] = None,
        exclude_paths: Optional[List[str]] = None,
        optional_paths: Optional[List[str]] = None,
    ) -> None:
        """Initialize the extension.

        Args:
            app: The Flask application (optional, can use init_app later).
            validator: The Primus Identity validator instance.
            exclude_paths: Paths to exclude from authentication.
            optional_paths: Paths where authentication is optional.
        """
        _ensure_flask()

        self.validator = validator
        self.exclude_paths = exclude_paths or ["/health", "/docs"]
        self.optional_paths = optional_paths or []

        if app is not None:
            self.init_app(app)

    def init_app(self, app: Flask) -> None:
        """Initialize the extension with a Flask app.

        Args:
            app: The Flask application.
        """
        if self.validator is None:
            raise ValueError("Validator must be provided before initializing with app")

        app.before_request(self._before_request)

    def _before_request(self) -> Optional[Tuple[Any, int]]:
        """Before request hook for authentication."""
        path = request.path

        # Check if path is excluded
        if any(path.startswith(p) for p in self.exclude_paths):
            g.primus_user = None
            return None

        is_optional = any(path.startswith(p) for p in self.optional_paths)

        # Check for Authorization header
        auth_header = request.headers.get("Authorization")

        if not auth_header:
            if is_optional:
                g.primus_user = None
                return None

            return jsonify({"detail": "Missing Authorization header"}), 401

        # Extract Bearer token
        if not auth_header.startswith("Bearer "):
            return (
                jsonify(
                    {"detail": "Invalid Authorization header format. Expected 'Bearer <token>'"}
                ),
                401,
            )

        token = auth_header[7:]

        # Validate token
        result = _run_async(self.validator.validate_token(token))

        if not result.is_valid:
            if is_optional:
                g.primus_user = None
                return None

            status_code = 401
            if result.error_code == "ISSUER_NOT_CONFIGURED":
                status_code = 403

            return (
                jsonify({"detail": result.error, "error_code": result.error_code}),
                status_code,
            )

        # Store user in Flask g object
        g.primus_user = result.user
        g.primus_validation_result = result

        return None


def get_current_user() -> Optional[PrimusUser]:
    """Get the current authenticated user from Flask g.

    Returns:
        The PrimusUser if authenticated, None otherwise.
    """
    _ensure_flask()
    return getattr(g, "primus_user", None)


def login_required(
    validator: PrimusIdentityValidator,
) -> Callable[[Callable[..., Any]], Callable[..., Any]]:
    """Decorator to require authentication for a route.

    Args:
        validator: The Primus Identity validator instance.

    Returns:
        A decorator function.

    Example:
        >>> @app.route("/protected")
        >>> @login_required(validator)
        >>> def protected_route():
        ...     user = get_current_user()
        ...     return {"user_id": user.sub}
    """
    _ensure_flask()

    def decorator(func: Callable[..., Any]) -> Callable[..., Any]:
        @wraps(func)
        def wrapper(*args: Any, **kwargs: Any) -> Any:
            auth_header = request.headers.get("Authorization")

            if not auth_header:
                return jsonify({"detail": "Missing Authorization header"}), 401

            if not auth_header.startswith("Bearer "):
                return (
                    jsonify({"detail": "Invalid Authorization header format"}),
                    401,
                )

            token = auth_header[7:]
            result = _run_async(validator.validate_token(token))

            if not result.is_valid:
                status_code = 401
                if result.error_code == "ISSUER_NOT_CONFIGURED":
                    status_code = 403
                return (
                    jsonify({"detail": result.error, "error_code": result.error_code}),
                    status_code,
                )

            g.primus_user = result.user
            return func(*args, **kwargs)

        return wrapper

    return decorator


def require_roles(
    *roles: str,
    validator: Optional[PrimusIdentityValidator] = None,
) -> Callable[[Callable[..., Any]], Callable[..., Any]]:
    """Decorator to require specific roles.

    Args:
        *roles: Required roles (user must have at least one).
        validator: Optional validator (uses g.primus_user if not provided).

    Returns:
        A decorator function.

    Example:
        >>> @app.route("/admin")
        >>> @require_roles("admin", "superuser", validator=validator)
        >>> def admin_route():
        ...     return {"message": "Welcome, admin!"}
    """
    _ensure_flask()

    def decorator(func: Callable[..., Any]) -> Callable[..., Any]:
        @wraps(func)
        def wrapper(*args: Any, **kwargs: Any) -> Any:
            # First ensure user is authenticated
            if validator:
                auth_header = request.headers.get("Authorization")

                if not auth_header or not auth_header.startswith("Bearer "):
                    return jsonify({"detail": "Authentication required"}), 401

                token = auth_header[7:]
                result = _run_async(validator.validate_token(token))

                if not result.is_valid:
                    return (
                        jsonify({"detail": result.error, "error_code": result.error_code}),
                        401,
                    )

                g.primus_user = result.user

            user = get_current_user()
            if user is None:
                return jsonify({"detail": "Authentication required"}), 401

            if not user.has_any_role(*roles):
                return (
                    jsonify({"detail": f"Required roles: {', '.join(roles)}"}),
                    403,
                )

            return func(*args, **kwargs)

        return wrapper

    return decorator


def require_permissions(
    *permissions: str,
    validator: Optional[PrimusIdentityValidator] = None,
) -> Callable[[Callable[..., Any]], Callable[..., Any]]:
    """Decorator to require specific permissions.

    Args:
        *permissions: Required permissions (user must have all).
        validator: Optional validator.

    Returns:
        A decorator function.
    """
    _ensure_flask()

    def decorator(func: Callable[..., Any]) -> Callable[..., Any]:
        @wraps(func)
        def wrapper(*args: Any, **kwargs: Any) -> Any:
            if validator:
                auth_header = request.headers.get("Authorization")

                if not auth_header or not auth_header.startswith("Bearer "):
                    return jsonify({"detail": "Authentication required"}), 401

                token = auth_header[7:]
                result = _run_async(validator.validate_token(token))

                if not result.is_valid:
                    return (
                        jsonify({"detail": result.error, "error_code": result.error_code}),
                        401,
                    )

                g.primus_user = result.user

            user = get_current_user()
            if user is None:
                return jsonify({"detail": "Authentication required"}), 401

            missing = [p for p in permissions if not user.has_permission(p)]
            if missing:
                return (
                    jsonify({"detail": f"Missing permissions: {', '.join(missing)}"}),
                    403,
                )

            return func(*args, **kwargs)

        return wrapper

    return decorator
