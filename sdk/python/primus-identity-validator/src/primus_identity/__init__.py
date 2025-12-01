"""
Primus Identity Validator - Multi-issuer JWT validation for Python applications.

This package provides enterprise-grade JWT validation with support for:
- Azure AD (Microsoft Entra ID)
- Auth0
- Google Identity
- AWS Cognito
- Local JWT (symmetric/asymmetric keys)

Example usage with FastAPI:
    from primus_identity import PrimusIdentityValidator, PrimusIdentityOptions, IssuerConfig
    from primus_identity.fastapi import primus_identity_middleware

    options = PrimusIdentityOptions(
        issuers=[
            IssuerConfig(
                name="AzureAD",
                type="oidc",
                issuer="https://login.microsoftonline.com/{tenant-id}/v2.0",
                authority="https://login.microsoftonline.com/{tenant-id}/v2.0",
                audiences=["api://your-client-id"]
            )
        ]
    )

    validator = PrimusIdentityValidator(options)
    app.add_middleware(primus_identity_middleware(validator))
"""

from primus_identity.types import (
    IssuerType,
    IssuerConfig,
    PrimusIdentityOptions,
    TokenValidationResult,
    PrimusUser,
    TenantContext,
    RateLimitingOptions,
    ValidationError,
    IssuerNotFoundError,
    TokenExpiredError,
    InvalidSignatureError,
    AudienceMismatchError,
)
from primus_identity.validator import PrimusIdentityValidator
from primus_identity.token_builder import TokenBuilder

__version__ = "1.0.0"
__all__ = [
    # Main classes
    "PrimusIdentityValidator",
    "TokenBuilder",
    # Configuration types
    "IssuerType",
    "IssuerConfig",
    "PrimusIdentityOptions",
    "RateLimitingOptions",
    # Result types
    "TokenValidationResult",
    "PrimusUser",
    "TenantContext",
    # Exceptions
    "ValidationError",
    "IssuerNotFoundError",
    "TokenExpiredError",
    "InvalidSignatureError",
    "AudienceMismatchError",
]
