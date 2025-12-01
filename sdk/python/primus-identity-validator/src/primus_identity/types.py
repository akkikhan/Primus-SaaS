"""
Type definitions for Primus Identity Validator.

Provides configuration classes and result types that mirror the .NET and Node.js SDKs.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from enum import Enum
from typing import Any, Callable, Dict, List, Literal, Optional, Union


class IssuerType(str, Enum):
    """Type of identity issuer."""

    OIDC = "oidc"
    """OpenID Connect issuer (Azure AD, Auth0, Google, Cognito)."""

    JWT = "jwt"
    """JWT issuer using shared secret or JWKS endpoint."""

    # Aliases for discoverability (same as OIDC)
    AZURE_AD = "oidc"
    AUTH0 = "oidc"
    GOOGLE = "oidc"
    COGNITO = "oidc"


@dataclass
class IssuerConfig:
    """Configuration for a single identity issuer.

    Attributes:
        name: Friendly name for this issuer (e.g., "AzureAD", "LocalAuth").
        type: Type of issuer ("oidc" or "jwt").
        issuer: The 'iss' claim value to match in the token.
        audiences: Valid audiences for this issuer.
        authority: For OIDC - The authority URL (e.g., https://login.microsoftonline.com/...).
        jwks_url: For JWT - The JWKS endpoint URL.
        secret: For JWT (Local Dev) - Shared secret key.
        claim_mappings: Optional claim mappings to normalize provider-specific claims.
        role_claim_name: Optional custom role claim name.
        permission_claim_name: Optional custom permission claim name.
        organization_claim_name: Optional organization claim name.
        validate_organization: Whether to enforce presence of the organization claim.
        required_organization: Optional required organization value.
        allow_machine_to_machine: Whether M2M tokens are allowed for this issuer.
        require_email_verification: Require email_verified=true for user tokens.
    """

    name: str
    type: Union[IssuerType, Literal["oidc", "jwt"]]
    issuer: str
    audiences: List[str]
    authority: Optional[str] = None
    jwks_url: Optional[str] = None
    secret: Optional[str] = None
    claim_mappings: Dict[str, str] = field(default_factory=dict)
    role_claim_name: Optional[str] = None
    permission_claim_name: str = "permissions"
    organization_claim_name: str = "org_id"
    validate_organization: bool = False
    required_organization: Optional[str] = None
    allow_machine_to_machine: bool = False
    allowed_grant_types: List[str] = field(default_factory=list)
    allowed_m2m_scopes: List[str] = field(default_factory=list)
    require_email_verification: bool = False

    def __post_init__(self) -> None:
        """Normalize type to string value."""
        if isinstance(self.type, IssuerType):
            self.type = self.type.value


@dataclass
class RateLimitingOptions:
    """Rate limiting options for failed token validations.

    Attributes:
        enabled: Whether rate limiting is enabled.
        max_failures_per_window: Maximum failed validations per client.
        max_global_failures_per_window: Global ceiling for all clients.
        window_seconds: Sliding window length in seconds.
        lockout_duration_seconds: How long to block a client after exceeding limit.
    """

    enabled: bool = False
    max_failures_per_window: int = 10
    max_global_failures_per_window: int = 100
    window_seconds: int = 60
    lockout_duration_seconds: int = 300


TenantResolverFunc = Callable[[Dict[str, Any]], Union["TenantContext", None]]
AsyncTenantResolverFunc = Callable[[Dict[str, Any]], "TenantContext"]


@dataclass
class PrimusIdentityOptions:
    """Configuration options for Primus SaaS identity validation.

    Attributes:
        issuers: List of trusted identity providers.
        require_https_metadata: Whether to require HTTPS for metadata endpoints.
        allow_http_on_localhost: Allow HTTP for localhost development.
        clock_skew_seconds: Clock skew tolerance in seconds.
        validate_lifetime: Whether to validate token lifetime.
        jwks_cache_ttl_hours: TTL for JWKS cache in hours.
        tenant_resolver: Optional function to resolve tenant context from claims.
        rate_limiting: Rate limiting configuration.
    """

    issuers: List[IssuerConfig]
    require_https_metadata: bool = True
    allow_http_on_localhost: bool = True
    clock_skew_seconds: int = 300
    validate_lifetime: bool = True
    jwks_cache_ttl_hours: int = 24
    tenant_resolver: Optional[TenantResolverFunc] = None
    rate_limiting: RateLimitingOptions = field(default_factory=RateLimitingOptions)

    def __post_init__(self) -> None:
        """Validate configuration on initialization."""
        if not self.issuers:
            raise ValueError("At least one issuer configuration is required")

        for issuer in self.issuers:
            self._validate_issuer(issuer)

    def _validate_issuer(self, issuer: IssuerConfig) -> None:
        """Validate a single issuer configuration."""
        if not issuer.name:
            raise ValueError("Issuer name is required")

        if not issuer.issuer:
            raise ValueError(f"Issuer URL is required for issuer: {issuer.name}")

        if not issuer.audiences:
            raise ValueError(f"At least one audience is required for issuer: {issuer.name}")

        issuer_type = issuer.type if isinstance(issuer.type, str) else issuer.type.value

        if issuer_type == "oidc":
            if not issuer.authority:
                raise ValueError(f"Authority URL is required for OIDC issuer: {issuer.name}")

            if self.require_https_metadata:
                authority_lower = issuer.authority.lower()
                is_localhost = any(
                    h in authority_lower
                    for h in ["localhost", "127.0.0.1", "::1", "[::1]"]
                )

                if not authority_lower.startswith("https://"):
                    if not (self.allow_http_on_localhost and is_localhost):
                        raise ValueError(
                            f"HTTPS is required for authority: {issuer.authority}. "
                            "Set require_https_metadata=False for development."
                        )

        elif issuer_type == "jwt":
            if not issuer.secret and not issuer.jwks_url:
                raise ValueError(
                    f"Either secret or jwks_url is required for JWT issuer: {issuer.name}"
                )


@dataclass
class TenantContext:
    """Represents the resolved tenant context.

    Attributes:
        tenant_id: The resolved tenant identifier.
        roles: List of roles for the user in this tenant.
        metadata: Additional tenant-specific metadata.
    """

    tenant_id: str
    roles: List[str] = field(default_factory=list)
    metadata: Dict[str, Any] = field(default_factory=dict)


@dataclass
class PrimusUser:
    """Represents an authenticated user with normalized claims.

    Attributes:
        sub: Subject identifier (user ID).
        email: User's email address.
        name: User's display name.
        roles: List of roles assigned to the user.
        permissions: List of permissions assigned to the user.
        organization_id: Organization/tenant identifier.
        issuer: The issuer that validated this user.
        issuer_name: Friendly name of the issuer configuration.
        claims: All original claims from the token.
        tenant_context: Optional resolved tenant context.
    """

    sub: str
    email: Optional[str] = None
    name: Optional[str] = None
    roles: List[str] = field(default_factory=list)
    permissions: List[str] = field(default_factory=list)
    organization_id: Optional[str] = None
    issuer: str = ""
    issuer_name: str = ""
    claims: Dict[str, Any] = field(default_factory=dict)
    tenant_context: Optional[TenantContext] = None

    @classmethod
    def from_claims(
        cls,
        claims: Dict[str, Any],
        issuer_config: IssuerConfig,
        tenant_context: Optional[TenantContext] = None,
    ) -> "PrimusUser":
        """Create a PrimusUser from JWT claims.

        Args:
            claims: The decoded JWT claims.
            issuer_config: The issuer configuration that validated the token.
            tenant_context: Optional resolved tenant context.

        Returns:
            A PrimusUser instance with normalized claims.
        """
        # Apply claim mappings
        mapped_claims = dict(claims)
        for source, target in issuer_config.claim_mappings.items():
            if source in mapped_claims:
                mapped_claims[target] = mapped_claims[source]

        # Extract standard claims
        sub = str(mapped_claims.get("sub", ""))

        # Email might be in different claims depending on provider
        email = mapped_claims.get("email") or mapped_claims.get("preferred_username")

        # Name variations
        name = (
            mapped_claims.get("name")
            or mapped_claims.get("given_name")
            or mapped_claims.get("nickname")
        )

        # Roles - check custom claim name or standard locations
        role_claim = issuer_config.role_claim_name or "roles"
        roles_value = mapped_claims.get(role_claim, [])
        if isinstance(roles_value, str):
            roles = [roles_value]
        elif isinstance(roles_value, list):
            roles = [str(r) for r in roles_value]
        else:
            roles = []

        # Permissions
        perm_claim = issuer_config.permission_claim_name
        perms_value = mapped_claims.get(perm_claim, [])
        if isinstance(perms_value, str):
            permissions = [perms_value]
        elif isinstance(perms_value, list):
            permissions = [str(p) for p in perms_value]
        else:
            permissions = []

        # Organization
        org_claim = issuer_config.organization_claim_name
        org_id = mapped_claims.get(org_claim) or mapped_claims.get("tenant_id")

        return cls(
            sub=sub,
            email=str(email) if email else None,
            name=str(name) if name else None,
            roles=roles,
            permissions=permissions,
            organization_id=str(org_id) if org_id else None,
            issuer=str(mapped_claims.get("iss", "")),
            issuer_name=issuer_config.name,
            claims=claims,
            tenant_context=tenant_context,
        )

    def has_role(self, role: str) -> bool:
        """Check if the user has a specific role."""
        return role in self.roles

    def has_any_role(self, *roles: str) -> bool:
        """Check if the user has any of the specified roles."""
        return any(role in self.roles for role in roles)

    def has_all_roles(self, *roles: str) -> bool:
        """Check if the user has all of the specified roles."""
        return all(role in self.roles for role in roles)

    def has_permission(self, permission: str) -> bool:
        """Check if the user has a specific permission."""
        return permission in self.permissions

    def has_scope(self, scope: str) -> bool:
        """Check if the token has a specific scope."""
        scp = self.claims.get("scp") or self.claims.get("scope", "")
        if isinstance(scp, str):
            return scope in scp.split()
        return False


@dataclass
class TokenValidationResult:
    """Result of token validation.

    Attributes:
        is_valid: Whether the token is valid.
        user: The authenticated user (if valid).
        claims: Raw claims from the token.
        error: Error message (if invalid).
        error_code: Error code for programmatic handling.
        issuer_config: The matched issuer configuration.
        tenant_context: Optional resolved tenant context.
    """

    is_valid: bool
    user: Optional[PrimusUser] = None
    claims: Optional[Dict[str, Any]] = None
    error: Optional[str] = None
    error_code: Optional[str] = None
    issuer_config: Optional[IssuerConfig] = None
    tenant_context: Optional[TenantContext] = None

    @classmethod
    def success(
        cls,
        claims: Dict[str, Any],
        issuer_config: IssuerConfig,
        tenant_context: Optional[TenantContext] = None,
    ) -> "TokenValidationResult":
        """Create a successful validation result."""
        user = PrimusUser.from_claims(claims, issuer_config, tenant_context)
        return cls(
            is_valid=True,
            user=user,
            claims=claims,
            issuer_config=issuer_config,
            tenant_context=tenant_context,
        )

    @classmethod
    def failure(cls, error: str, error_code: str = "VALIDATION_ERROR") -> "TokenValidationResult":
        """Create a failed validation result."""
        return cls(is_valid=False, error=error, error_code=error_code)


# Exceptions
class ValidationError(Exception):
    """Base exception for validation errors."""

    def __init__(self, message: str, error_code: str = "VALIDATION_ERROR") -> None:
        super().__init__(message)
        self.error_code = error_code


class IssuerNotFoundError(ValidationError):
    """Raised when no matching issuer configuration is found."""

    def __init__(self, issuer: str) -> None:
        super().__init__(
            f"Untrusted issuer: {issuer}. No matching configuration found.",
            "ISSUER_NOT_CONFIGURED",
        )
        self.issuer = issuer


class TokenExpiredError(ValidationError):
    """Raised when the token has expired."""

    def __init__(self) -> None:
        super().__init__("Token has expired", "TOKEN_EXPIRED")


class InvalidSignatureError(ValidationError):
    """Raised when the token signature is invalid."""

    def __init__(self) -> None:
        super().__init__("Invalid token signature", "SIGNATURE_INVALID")


class AudienceMismatchError(ValidationError):
    """Raised when the token audience doesn't match."""

    def __init__(self, expected: List[str], actual: str) -> None:
        super().__init__(
            f"Token audience '{actual}' does not match expected: {expected}",
            "AUDIENCE_MISMATCH",
        )
        self.expected = expected
        self.actual = actual
