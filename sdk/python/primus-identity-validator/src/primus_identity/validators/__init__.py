"""
JWT Token Validators for different issuer types.
"""

from __future__ import annotations

import time
from abc import ABC, abstractmethod
from typing import Any, Dict, List, Optional, Tuple

import jwt
from jwt import PyJWK
from jwt.exceptions import (
    DecodeError,
    ExpiredSignatureError,
    InvalidAudienceError,
    InvalidIssuerError,
    InvalidSignatureError as JWTInvalidSignatureError,
)

from primus_identity.services.jwks_service import JwksService, OpenIdConfigurationService
from primus_identity.types import (
    AudienceMismatchError,
    IssuerConfig,
    InvalidSignatureError,
    TokenExpiredError,
    TokenValidationResult,
    ValidationError,
)


class BaseValidator(ABC):
    """Base class for token validators."""

    @abstractmethod
    async def validate(
        self,
        token: str,
        issuer_config: IssuerConfig,
        validate_lifetime: bool = True,
        clock_skew_seconds: int = 300,
    ) -> TokenValidationResult:
        """Validate a JWT token.

        Args:
            token: The JWT token to validate.
            issuer_config: The issuer configuration.
            validate_lifetime: Whether to validate token expiration.
            clock_skew_seconds: Allowed clock skew in seconds.

        Returns:
            Token validation result.
        """
        pass

    def _decode_unverified(self, token: str) -> Tuple[Dict[str, Any], Dict[str, Any]]:
        """Decode a token without verification to inspect claims.

        Args:
            token: The JWT token.

        Returns:
            Tuple of (header, payload).
        """
        parts = token.split(".")
        if len(parts) != 3:
            raise DecodeError("Invalid token format")

        header = jwt.get_unverified_header(token)
        payload = jwt.decode(token, options={"verify_signature": False})
        return header, payload


class LocalValidator(BaseValidator):
    """Validator for local JWT tokens using symmetric secrets."""

    async def validate(
        self,
        token: str,
        issuer_config: IssuerConfig,
        validate_lifetime: bool = True,
        clock_skew_seconds: int = 300,
    ) -> TokenValidationResult:
        """Validate a local JWT token.

        Args:
            token: The JWT token to validate.
            issuer_config: The issuer configuration with secret.
            validate_lifetime: Whether to validate token expiration.
            clock_skew_seconds: Allowed clock skew in seconds.

        Returns:
            Token validation result.
        """
        if not issuer_config.secret:
            return TokenValidationResult.failure(
                f"Secret is required for JWT issuer: {issuer_config.name}",
                "CONFIGURATION_ERROR",
            )

        try:
            options = {
                "verify_signature": True,
                "verify_exp": validate_lifetime,
                "verify_aud": True,
                "verify_iss": True,
                "require": ["exp", "iat", "iss"],
            }

            claims = jwt.decode(
                token,
                issuer_config.secret,
                algorithms=["HS256", "HS384", "HS512"],
                audience=issuer_config.audiences,
                issuer=issuer_config.issuer,
                leeway=clock_skew_seconds,
                options=options,
            )

            return TokenValidationResult.success(claims, issuer_config)

        except ExpiredSignatureError:
            return TokenValidationResult.failure("Token has expired", "TOKEN_EXPIRED")
        except InvalidAudienceError as e:
            return TokenValidationResult.failure(str(e), "AUDIENCE_MISMATCH")
        except InvalidIssuerError as e:
            return TokenValidationResult.failure(str(e), "ISSUER_MISMATCH")
        except JWTInvalidSignatureError:
            return TokenValidationResult.failure("Invalid token signature", "SIGNATURE_INVALID")
        except DecodeError as e:
            return TokenValidationResult.failure(f"Invalid token format: {e}", "INVALID_TOKEN")
        except Exception as e:
            return TokenValidationResult.failure(f"Validation error: {e}", "VALIDATION_ERROR")


class OidcValidator(BaseValidator):
    """Validator for OIDC tokens (Azure AD, Auth0, Google, Cognito)."""

    def __init__(
        self,
        oidc_service: OpenIdConfigurationService,
        jwks_service: JwksService,
    ) -> None:
        """Initialize the OIDC validator.

        Args:
            oidc_service: Service for fetching OpenID configuration.
            jwks_service: Service for fetching JWKS.
        """
        self._oidc_service = oidc_service
        self._jwks_service = jwks_service

    async def validate(
        self,
        token: str,
        issuer_config: IssuerConfig,
        validate_lifetime: bool = True,
        clock_skew_seconds: int = 300,
    ) -> TokenValidationResult:
        """Validate an OIDC token.

        Args:
            token: The JWT token to validate.
            issuer_config: The issuer configuration with authority.
            validate_lifetime: Whether to validate token expiration.
            clock_skew_seconds: Allowed clock skew in seconds.

        Returns:
            Token validation result.
        """
        if not issuer_config.authority:
            return TokenValidationResult.failure(
                f"Authority URL is required for OIDC issuer: {issuer_config.name}",
                "CONFIGURATION_ERROR",
            )

        try:
            # Get unverified header to find kid
            header, _ = self._decode_unverified(token)
            kid = header.get("kid")

            # Fetch JWKS URI from OpenID configuration
            jwks_uri = await self._oidc_service.get_jwks_uri(issuer_config.authority)

            # Get signing key
            signing_key = await self._jwks_service.get_signing_key(jwks_uri, kid)

            # Determine algorithm from key or header
            algorithm = header.get("alg", "RS256")
            if algorithm not in ["RS256", "RS384", "RS512", "ES256", "ES384", "ES512"]:
                return TokenValidationResult.failure(
                    f"Unsupported algorithm: {algorithm}", "UNSUPPORTED_ALGORITHM"
                )

            options = {
                "verify_signature": True,
                "verify_exp": validate_lifetime,
                "verify_aud": True,
                "verify_iss": True,
                "require": ["exp", "iat", "iss"],
            }

            claims = jwt.decode(
                token,
                signing_key.key,
                algorithms=[algorithm],
                audience=issuer_config.audiences,
                issuer=issuer_config.issuer,
                leeway=clock_skew_seconds,
                options=options,
            )

            return TokenValidationResult.success(claims, issuer_config)

        except ExpiredSignatureError:
            return TokenValidationResult.failure("Token has expired", "TOKEN_EXPIRED")
        except InvalidAudienceError as e:
            return TokenValidationResult.failure(str(e), "AUDIENCE_MISMATCH")
        except InvalidIssuerError as e:
            return TokenValidationResult.failure(str(e), "ISSUER_MISMATCH")
        except JWTInvalidSignatureError:
            return TokenValidationResult.failure("Invalid token signature", "SIGNATURE_INVALID")
        except DecodeError as e:
            return TokenValidationResult.failure(f"Invalid token format: {e}", "INVALID_TOKEN")
        except Exception as e:
            return TokenValidationResult.failure(f"Validation error: {e}", "VALIDATION_ERROR")


class JwksValidator(BaseValidator):
    """Validator for JWT tokens using JWKS endpoints (non-OIDC)."""

    def __init__(self, jwks_service: JwksService) -> None:
        """Initialize the JWKS validator.

        Args:
            jwks_service: Service for fetching JWKS.
        """
        self._jwks_service = jwks_service

    async def validate(
        self,
        token: str,
        issuer_config: IssuerConfig,
        validate_lifetime: bool = True,
        clock_skew_seconds: int = 300,
    ) -> TokenValidationResult:
        """Validate a JWT token using JWKS.

        Args:
            token: The JWT token to validate.
            issuer_config: The issuer configuration with jwks_url.
            validate_lifetime: Whether to validate token expiration.
            clock_skew_seconds: Allowed clock skew in seconds.

        Returns:
            Token validation result.
        """
        if not issuer_config.jwks_url:
            return TokenValidationResult.failure(
                f"JWKS URL is required for JWT issuer: {issuer_config.name}",
                "CONFIGURATION_ERROR",
            )

        try:
            # Get unverified header to find kid
            header, _ = self._decode_unverified(token)
            kid = header.get("kid")

            # Get signing key
            signing_key = await self._jwks_service.get_signing_key(issuer_config.jwks_url, kid)

            # Determine algorithm
            algorithm = header.get("alg", "RS256")

            options = {
                "verify_signature": True,
                "verify_exp": validate_lifetime,
                "verify_aud": True,
                "verify_iss": True,
                "require": ["exp", "iat", "iss"],
            }

            claims = jwt.decode(
                token,
                signing_key.key,
                algorithms=[algorithm],
                audience=issuer_config.audiences,
                issuer=issuer_config.issuer,
                leeway=clock_skew_seconds,
                options=options,
            )

            return TokenValidationResult.success(claims, issuer_config)

        except ExpiredSignatureError:
            return TokenValidationResult.failure("Token has expired", "TOKEN_EXPIRED")
        except InvalidAudienceError as e:
            return TokenValidationResult.failure(str(e), "AUDIENCE_MISMATCH")
        except InvalidIssuerError as e:
            return TokenValidationResult.failure(str(e), "ISSUER_MISMATCH")
        except JWTInvalidSignatureError:
            return TokenValidationResult.failure("Invalid token signature", "SIGNATURE_INVALID")
        except DecodeError as e:
            return TokenValidationResult.failure(f"Invalid token format: {e}", "INVALID_TOKEN")
        except Exception as e:
            return TokenValidationResult.failure(f"Validation error: {e}", "VALIDATION_ERROR")
