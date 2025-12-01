"""
Token builder for creating JWT tokens (useful for testing and local development).
"""

from __future__ import annotations

import time
import uuid
from dataclasses import dataclass, field
from typing import Any, Dict, List, Optional

import jwt


@dataclass
class TokenBuilder:
    """Builder for creating JWT tokens for testing and local development.

    This is useful for:
    - Unit testing your token validation logic
    - Local development with mock authentication
    - Integration testing

    Example:
        >>> builder = TokenBuilder(
        ...     secret="my-secret-key",
        ...     issuer="https://auth.local",
        ...     audience="api://my-api"
        ... )
        >>> token = builder.build(
        ...     sub="user-123",
        ...     email="user@example.com",
        ...     roles=["admin"]
        ... )
    """

    secret: str
    """The signing secret (for HS256) or private key (for RS256)."""

    issuer: str
    """The issuer URL for the token."""

    audience: str
    """The audience for the token."""

    algorithm: str = "HS256"
    """The signing algorithm (default: HS256)."""

    expires_in_seconds: int = 3600
    """Token expiration time in seconds (default: 1 hour)."""

    default_claims: Dict[str, Any] = field(default_factory=dict)
    """Default claims to include in all tokens."""

    def build(
        self,
        sub: str,
        email: Optional[str] = None,
        name: Optional[str] = None,
        roles: Optional[List[str]] = None,
        permissions: Optional[List[str]] = None,
        scopes: Optional[List[str]] = None,
        organization_id: Optional[str] = None,
        tenant_id: Optional[str] = None,
        expires_in: Optional[int] = None,
        additional_claims: Optional[Dict[str, Any]] = None,
        **kwargs: Any,
    ) -> str:
        """Build a JWT token with the specified claims.

        Args:
            sub: Subject identifier (user ID).
            email: User's email address.
            name: User's display name.
            roles: List of roles.
            permissions: List of permissions.
            scopes: List of OAuth scopes.
            organization_id: Organization/tenant identifier.
            tenant_id: Alias for organization_id.
            expires_in: Override expiration time in seconds.
            additional_claims: Additional custom claims.
            **kwargs: Additional claims as keyword arguments.

        Returns:
            The signed JWT token.
        """
        now = int(time.time())
        exp_seconds = expires_in or self.expires_in_seconds

        # Build claims
        claims: Dict[str, Any] = {
            **self.default_claims,
            "iss": self.issuer,
            "aud": self.audience,
            "sub": sub,
            "iat": now,
            "nbf": now,
            "exp": now + exp_seconds,
            "jti": str(uuid.uuid4()),
        }

        # Add optional claims
        if email:
            claims["email"] = email

        if name:
            claims["name"] = name

        if roles:
            claims["roles"] = roles

        if permissions:
            claims["permissions"] = permissions

        if scopes:
            claims["scp"] = " ".join(scopes)
            claims["scope"] = " ".join(scopes)

        org_id = organization_id or tenant_id
        if org_id:
            claims["org_id"] = org_id
            claims["tenant_id"] = org_id

        # Add additional claims
        if additional_claims:
            claims.update(additional_claims)

        claims.update(kwargs)

        return jwt.encode(claims, self.secret, algorithm=self.algorithm)

    def build_m2m_token(
        self,
        client_id: str,
        scopes: Optional[List[str]] = None,
        permissions: Optional[List[str]] = None,
        expires_in: Optional[int] = None,
        additional_claims: Optional[Dict[str, Any]] = None,
    ) -> str:
        """Build a machine-to-machine (client credentials) token.

        Args:
            client_id: The client application ID.
            scopes: List of OAuth scopes.
            permissions: List of permissions.
            expires_in: Override expiration time in seconds.
            additional_claims: Additional custom claims.

        Returns:
            The signed JWT token.
        """
        now = int(time.time())
        exp_seconds = expires_in or self.expires_in_seconds

        claims: Dict[str, Any] = {
            **self.default_claims,
            "iss": self.issuer,
            "aud": self.audience,
            "sub": client_id,
            "azp": client_id,
            "gty": "client-credentials",
            "iat": now,
            "nbf": now,
            "exp": now + exp_seconds,
            "jti": str(uuid.uuid4()),
        }

        if scopes:
            claims["scp"] = " ".join(scopes)
            claims["scope"] = " ".join(scopes)

        if permissions:
            claims["permissions"] = permissions

        if additional_claims:
            claims.update(additional_claims)

        return jwt.encode(claims, self.secret, algorithm=self.algorithm)

    def build_expired_token(
        self,
        sub: str,
        expired_seconds_ago: int = 3600,
        **kwargs: Any,
    ) -> str:
        """Build an expired token for testing.

        Args:
            sub: Subject identifier.
            expired_seconds_ago: How many seconds ago the token expired.
            **kwargs: Additional claims.

        Returns:
            The signed (expired) JWT token.
        """
        now = int(time.time())

        claims: Dict[str, Any] = {
            **self.default_claims,
            "iss": self.issuer,
            "aud": self.audience,
            "sub": sub,
            "iat": now - expired_seconds_ago - 3600,
            "nbf": now - expired_seconds_ago - 3600,
            "exp": now - expired_seconds_ago,
            "jti": str(uuid.uuid4()),
            **kwargs,
        }

        return jwt.encode(claims, self.secret, algorithm=self.algorithm)

    @staticmethod
    def decode_unverified(token: str) -> Dict[str, Any]:
        """Decode a token without verification (for debugging).

        Args:
            token: The JWT token.

        Returns:
            The decoded claims.
        """
        return jwt.decode(token, options={"verify_signature": False})
