"""
Main validator class for Primus SaaS identity validation.
"""

from __future__ import annotations

import asyncio
import logging
from typing import Any, Dict, Optional

import jwt as pyjwt
from jwt.exceptions import DecodeError

from primus_identity.services.jwks_service import (
    JwksCache,
    JwksService,
    OpenIdConfigurationService,
)
from primus_identity.types import (
    IssuerConfig,
    IssuerNotFoundError,
    PrimusIdentityOptions,
    PrimusUser,
    TenantContext,
    TokenValidationResult,
    ValidationError,
)
from primus_identity.validators import JwksValidator, LocalValidator, OidcValidator

logger = logging.getLogger("primus_identity")


class PrimusIdentityValidator:
    """Main validator class for Primus SaaS identity validation.

    This class provides multi-issuer JWT validation with support for:
    - Azure AD (Microsoft Entra ID)
    - Auth0
    - Google Identity
    - AWS Cognito
    - Local JWT (symmetric/asymmetric keys)

    Example:
        >>> options = PrimusIdentityOptions(
        ...     issuers=[
        ...         IssuerConfig(
        ...             name="LocalAuth",
        ...             type="jwt",
        ...             issuer="https://auth.local",
        ...             secret="my-secret-key",
        ...             audiences=["api://my-api"]
        ...         )
        ...     ]
        ... )
        >>> validator = PrimusIdentityValidator(options)
        >>> result = await validator.validate_token(token)
        >>> if result.is_valid:
        ...     print(f"User: {result.user.sub}")
    """

    def __init__(self, options: PrimusIdentityOptions) -> None:
        """Initialize the validator with configuration options.

        Args:
            options: Configuration options including issuers.

        Raises:
            ValueError: If options are invalid.
        """
        self._options = options

        # Initialize caches and services
        self._jwks_cache = JwksCache(ttl_hours=options.jwks_cache_ttl_hours)
        self._jwks_service = JwksService(self._jwks_cache)
        self._oidc_service = OpenIdConfigurationService(
            cache_ttl_hours=options.jwks_cache_ttl_hours
        )

        # Initialize validators
        self._local_validator = LocalValidator()
        self._oidc_validator = OidcValidator(self._oidc_service, self._jwks_service)
        self._jwks_validator = JwksValidator(self._jwks_service)

        # Build issuer lookup map
        self._issuer_map: Dict[str, IssuerConfig] = {}
        for issuer in options.issuers:
            self._issuer_map[issuer.issuer] = issuer

        logger.info(
            "PrimusIdentityValidator initialized with %d issuer(s): %s",
            len(options.issuers),
            ", ".join(i.name for i in options.issuers),
        )

    @property
    def options(self) -> PrimusIdentityOptions:
        """Get the current configuration options."""
        return self._options

    async def validate_token(self, token: str) -> TokenValidationResult:
        """Validate a JWT token based on configured issuers.

        This method:
        1. Decodes the token to find the issuer claim
        2. Routes to the appropriate validator based on issuer configuration
        3. Validates the token signature, expiration, and audience
        4. Optionally resolves tenant context if configured

        Args:
            token: The JWT token to validate.

        Returns:
            TokenValidationResult with validation status and user info.
        """
        try:
            # 1. Decode token to find issuer (without verification)
            try:
                unverified = pyjwt.decode(token, options={"verify_signature": False})
            except DecodeError as e:
                logger.warning("Failed to decode token: %s", e)
                return TokenValidationResult.failure(
                    f"Invalid token format: {e}", "INVALID_TOKEN"
                )

            issuer = unverified.get("iss")
            if not issuer:
                logger.warning("Token missing issuer (iss) claim")
                return TokenValidationResult.failure(
                    "Token missing issuer (iss) claim", "MISSING_ISSUER"
                )

            # 2. Find matching issuer configuration
            issuer_config = self._issuer_map.get(issuer)
            if not issuer_config:
                logger.warning("Untrusted issuer: %s", issuer)
                return TokenValidationResult.failure(
                    f"Untrusted issuer: {issuer}. No matching configuration found.",
                    "ISSUER_NOT_CONFIGURED",
                )

            # 3. Route to appropriate validator
            issuer_type = (
                issuer_config.type
                if isinstance(issuer_config.type, str)
                else issuer_config.type.value
            )

            if issuer_type == "oidc":
                result = await self._oidc_validator.validate(
                    token,
                    issuer_config,
                    validate_lifetime=self._options.validate_lifetime,
                    clock_skew_seconds=self._options.clock_skew_seconds,
                )
            elif issuer_type == "jwt":
                if issuer_config.jwks_url:
                    result = await self._jwks_validator.validate(
                        token,
                        issuer_config,
                        validate_lifetime=self._options.validate_lifetime,
                        clock_skew_seconds=self._options.clock_skew_seconds,
                    )
                else:
                    result = await self._local_validator.validate(
                        token,
                        issuer_config,
                        validate_lifetime=self._options.validate_lifetime,
                        clock_skew_seconds=self._options.clock_skew_seconds,
                    )
            else:
                return TokenValidationResult.failure(
                    f"Unsupported issuer type: {issuer_type}", "UNSUPPORTED_ISSUER_TYPE"
                )

            # 4. Resolve tenant context if configured
            if result.is_valid and self._options.tenant_resolver and result.claims:
                try:
                    tenant_context = self._options.tenant_resolver(result.claims)
                    if asyncio.iscoroutine(tenant_context):
                        tenant_context = await tenant_context

                    if tenant_context:
                        result.tenant_context = tenant_context
                        if result.user:
                            result.user.tenant_context = tenant_context

                    logger.debug("Tenant context resolved: %s", tenant_context)
                except Exception as e:
                    logger.error("Tenant resolution failed: %s", e)
                    return TokenValidationResult.failure(
                        f"Tenant resolution failed: {e}", "TENANT_RESOLUTION_FAILED"
                    )

            if result.is_valid:
                logger.debug(
                    "Token validated successfully for issuer: %s, sub: %s",
                    issuer_config.name,
                    result.user.sub if result.user else "unknown",
                )
            else:
                logger.warning(
                    "Token validation failed for issuer %s: %s",
                    issuer_config.name,
                    result.error,
                )

            return result

        except Exception as e:
            logger.exception("Unexpected validation error: %s", e)
            return TokenValidationResult.failure(
                f"Unexpected validation error: {e}", "VALIDATION_ERROR"
            )

    def validate_token_sync(self, token: str) -> TokenValidationResult:
        """Synchronous wrapper for validate_token.

        Args:
            token: The JWT token to validate.

        Returns:
            TokenValidationResult with validation status and user info.
        """
        return asyncio.get_event_loop().run_until_complete(self.validate_token(token))

    def find_issuer_config(self, issuer: str) -> Optional[IssuerConfig]:
        """Find the issuer configuration for a given issuer URL.

        Args:
            issuer: The issuer URL to find.

        Returns:
            The matching IssuerConfig or None.
        """
        return self._issuer_map.get(issuer)

    async def close(self) -> None:
        """Close resources (HTTP clients, caches)."""
        await self._jwks_service.close()
        await self._oidc_service.close()
        self._jwks_cache.clear()
        logger.info("PrimusIdentityValidator closed")

    async def __aenter__(self) -> "PrimusIdentityValidator":
        """Async context manager entry."""
        return self

    async def __aexit__(self, exc_type: Any, exc_val: Any, exc_tb: Any) -> None:
        """Async context manager exit."""
        await self.close()
