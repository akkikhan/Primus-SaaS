"""
Tests for Primus Identity Validator - Local JWT validation.
"""

import time

import jwt
import pytest

from primus_identity import (
    IssuerConfig,
    PrimusIdentityOptions,
    PrimusIdentityValidator,
    TokenBuilder,
    TokenValidationResult,
)


def build_local_options() -> PrimusIdentityOptions:
    """Build options with a single local JWT issuer."""
    return PrimusIdentityOptions(
        issuers=[
            IssuerConfig(
                name="LocalAuth",
                type="jwt",
                issuer="https://auth.local",
                secret="local-secret-key-for-testing",
                audiences=["api://my-api"],
            )
        ],
        clock_skew_seconds=300,
        validate_lifetime=True,
    )


def build_multi_issuer_options() -> PrimusIdentityOptions:
    """Build options with multiple issuers."""
    return PrimusIdentityOptions(
        issuers=[
            IssuerConfig(
                name="LocalAuth",
                type="jwt",
                issuer="https://auth.local",
                secret="local-secret-key",
                audiences=["api://my-api"],
            ),
            IssuerConfig(
                name="TestAuth",
                type="jwt",
                issuer="https://test.auth.local",
                secret="test-secret-key",
                audiences=["api://test-api"],
            ),
        ],
        clock_skew_seconds=300,
        validate_lifetime=True,
    )


class TestLocalJwtValidation:
    """Tests for local JWT validation."""

    @pytest.fixture
    def validator(self) -> PrimusIdentityValidator:
        return PrimusIdentityValidator(build_local_options())

    @pytest.fixture
    def token_builder(self) -> TokenBuilder:
        return TokenBuilder(
            secret="local-secret-key-for-testing",
            issuer="https://auth.local",
            audience="api://my-api",
        )

    @pytest.mark.asyncio
    async def test_accepts_valid_token(
        self, validator: PrimusIdentityValidator, token_builder: TokenBuilder
    ) -> None:
        """Test that a valid token is accepted."""
        token = token_builder.build(
            sub="user-123",
            email="user@example.com",
            name="Test User",
        )

        result = await validator.validate_token(token)

        assert result.is_valid is True
        assert result.user is not None
        assert result.user.sub == "user-123"
        assert result.user.email == "user@example.com"
        assert result.user.name == "Test User"

    @pytest.mark.asyncio
    async def test_accepts_token_with_roles(
        self, validator: PrimusIdentityValidator, token_builder: TokenBuilder
    ) -> None:
        """Test that roles are correctly extracted."""
        token = token_builder.build(
            sub="user-123",
            roles=["admin", "user"],
        )

        result = await validator.validate_token(token)

        assert result.is_valid is True
        assert result.user is not None
        assert result.user.has_role("admin")
        assert result.user.has_role("user")
        assert not result.user.has_role("superuser")
        assert result.user.has_any_role("admin", "superuser")
        assert result.user.has_all_roles("admin", "user")

    @pytest.mark.asyncio
    async def test_accepts_token_with_permissions(
        self, validator: PrimusIdentityValidator, token_builder: TokenBuilder
    ) -> None:
        """Test that permissions are correctly extracted."""
        token = token_builder.build(
            sub="user-123",
            permissions=["read:users", "write:users"],
        )

        result = await validator.validate_token(token)

        assert result.is_valid is True
        assert result.user is not None
        assert result.user.has_permission("read:users")
        assert result.user.has_permission("write:users")
        assert not result.user.has_permission("delete:users")

    @pytest.mark.asyncio
    async def test_rejects_wrong_secret(
        self, validator: PrimusIdentityValidator
    ) -> None:
        """Test that a token signed with the wrong secret is rejected."""
        wrong_builder = TokenBuilder(
            secret="wrong-secret",
            issuer="https://auth.local",
            audience="api://my-api",
        )
        token = wrong_builder.build(sub="user-123")

        result = await validator.validate_token(token)

        assert result.is_valid is False
        assert result.error is not None
        assert "signature" in result.error.lower()

    @pytest.mark.asyncio
    async def test_rejects_unknown_issuer(
        self, validator: PrimusIdentityValidator
    ) -> None:
        """Test that a token from an unknown issuer is rejected."""
        unknown_builder = TokenBuilder(
            secret="some-secret",
            issuer="https://unknown.issuer",
            audience="api://my-api",
        )
        token = unknown_builder.build(sub="user-123")

        result = await validator.validate_token(token)

        assert result.is_valid is False
        assert result.error_code == "ISSUER_NOT_CONFIGURED"
        assert "untrusted issuer" in result.error.lower()

    @pytest.mark.asyncio
    async def test_rejects_expired_token(
        self, validator: PrimusIdentityValidator, token_builder: TokenBuilder
    ) -> None:
        """Test that an expired token is rejected."""
        token = token_builder.build_expired_token(
            sub="user-123",
            expired_seconds_ago=3600,  # Expired 1 hour ago
        )

        result = await validator.validate_token(token)

        assert result.is_valid is False
        assert result.error_code == "TOKEN_EXPIRED"

    @pytest.mark.asyncio
    async def test_rejects_wrong_audience(
        self, validator: PrimusIdentityValidator
    ) -> None:
        """Test that a token with wrong audience is rejected."""
        wrong_aud_builder = TokenBuilder(
            secret="local-secret-key-for-testing",
            issuer="https://auth.local",
            audience="api://wrong-api",  # Wrong audience
        )
        token = wrong_aud_builder.build(sub="user-123")

        result = await validator.validate_token(token)

        assert result.is_valid is False
        assert result.error_code == "AUDIENCE_MISMATCH"

    @pytest.mark.asyncio
    async def test_rejects_invalid_token_format(
        self, validator: PrimusIdentityValidator
    ) -> None:
        """Test that an invalid token format is rejected."""
        result = await validator.validate_token("not-a-valid-token")

        assert result.is_valid is False
        assert "invalid" in result.error.lower()


class TestMultiIssuerValidation:
    """Tests for multi-issuer validation."""

    @pytest.fixture
    def validator(self) -> PrimusIdentityValidator:
        return PrimusIdentityValidator(build_multi_issuer_options())

    @pytest.mark.asyncio
    async def test_routes_to_correct_issuer(
        self, validator: PrimusIdentityValidator
    ) -> None:
        """Test that tokens are routed to the correct issuer."""
        # Token from first issuer
        builder1 = TokenBuilder(
            secret="local-secret-key",
            issuer="https://auth.local",
            audience="api://my-api",
        )
        token1 = builder1.build(sub="user-1")

        result1 = await validator.validate_token(token1)
        assert result1.is_valid is True
        assert result1.user.issuer_name == "LocalAuth"

        # Token from second issuer
        builder2 = TokenBuilder(
            secret="test-secret-key",
            issuer="https://test.auth.local",
            audience="api://test-api",
        )
        token2 = builder2.build(sub="user-2")

        result2 = await validator.validate_token(token2)
        assert result2.is_valid is True
        assert result2.user.issuer_name == "TestAuth"


class TestOptionsValidation:
    """Tests for options validation."""

    def test_requires_at_least_one_issuer(self) -> None:
        """Test that at least one issuer is required."""
        with pytest.raises(ValueError, match="At least one issuer"):
            PrimusIdentityOptions(issuers=[])

    def test_requires_issuer_name(self) -> None:
        """Test that issuer name is required."""
        with pytest.raises(ValueError, match="Issuer name is required"):
            PrimusIdentityOptions(
                issuers=[
                    IssuerConfig(
                        name="",
                        type="jwt",
                        issuer="https://auth.local",
                        secret="secret",
                        audiences=["api://my-api"],
                    )
                ]
            )

    def test_requires_secret_or_jwks_for_jwt(self) -> None:
        """Test that secret or JWKS URL is required for JWT issuers."""
        with pytest.raises(ValueError, match="secret or jwks_url is required"):
            PrimusIdentityOptions(
                issuers=[
                    IssuerConfig(
                        name="TestAuth",
                        type="jwt",
                        issuer="https://auth.local",
                        audiences=["api://my-api"],
                        # Missing both secret and jwks_url
                    )
                ]
            )

    def test_requires_authority_for_oidc(self) -> None:
        """Test that authority is required for OIDC issuers."""
        with pytest.raises(ValueError, match="Authority URL is required"):
            PrimusIdentityOptions(
                issuers=[
                    IssuerConfig(
                        name="AzureAD",
                        type="oidc",
                        issuer="https://login.microsoftonline.com/tenant/v2.0",
                        audiences=["api://my-api"],
                        # Missing authority
                    )
                ]
            )


class TestTenantResolver:
    """Tests for tenant resolution."""

    @pytest.mark.asyncio
    async def test_resolves_tenant_context(self) -> None:
        """Test that tenant context is resolved."""
        from primus_identity import TenantContext

        def resolve_tenant(claims: dict) -> TenantContext:
            org_id = claims.get("org_id", "default")
            return TenantContext(
                tenant_id=str(org_id),
                roles=["tenant-user"],
                metadata={"plan": "enterprise"},
            )

        options = PrimusIdentityOptions(
            issuers=[
                IssuerConfig(
                    name="LocalAuth",
                    type="jwt",
                    issuer="https://auth.local",
                    secret="secret-key",
                    audiences=["api://my-api"],
                )
            ],
            tenant_resolver=resolve_tenant,
        )

        validator = PrimusIdentityValidator(options)
        builder = TokenBuilder(
            secret="secret-key",
            issuer="https://auth.local",
            audience="api://my-api",
        )
        token = builder.build(sub="user-123", organization_id="org-456")

        result = await validator.validate_token(token)

        assert result.is_valid is True
        assert result.tenant_context is not None
        assert result.tenant_context.tenant_id == "org-456"
        assert result.user.tenant_context is not None


class TestTokenBuilder:
    """Tests for the TokenBuilder utility."""

    def test_builds_valid_token(self) -> None:
        """Test that TokenBuilder creates valid tokens."""
        builder = TokenBuilder(
            secret="test-secret",
            issuer="https://auth.local",
            audience="api://my-api",
        )

        token = builder.build(sub="user-123", email="test@example.com")
        decoded = TokenBuilder.decode_unverified(token)

        assert decoded["sub"] == "user-123"
        assert decoded["email"] == "test@example.com"
        assert decoded["iss"] == "https://auth.local"
        assert decoded["aud"] == "api://my-api"

    def test_builds_m2m_token(self) -> None:
        """Test that TokenBuilder creates M2M tokens."""
        builder = TokenBuilder(
            secret="test-secret",
            issuer="https://auth.local",
            audience="api://my-api",
        )

        token = builder.build_m2m_token(
            client_id="client-123",
            scopes=["read:data", "write:data"],
        )
        decoded = TokenBuilder.decode_unverified(token)

        assert decoded["sub"] == "client-123"
        assert decoded["gty"] == "client-credentials"
        assert "read:data" in decoded["scp"]

    def test_builds_expired_token(self) -> None:
        """Test that TokenBuilder creates expired tokens."""
        builder = TokenBuilder(
            secret="test-secret",
            issuer="https://auth.local",
            audience="api://my-api",
        )

        token = builder.build_expired_token(sub="user-123", expired_seconds_ago=60)
        decoded = TokenBuilder.decode_unverified(token)

        assert decoded["exp"] < time.time()
