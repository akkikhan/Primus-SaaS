"""
Tests for PrimusUser claim extraction and helpers.
"""

import pytest

from primus_identity import IssuerConfig, PrimusUser


class TestPrimusUserCreation:
    """Tests for PrimusUser.from_claims()."""

    @pytest.fixture
    def basic_issuer(self) -> IssuerConfig:
        return IssuerConfig(
            name="TestAuth",
            type="jwt",
            issuer="https://auth.test",
            secret="secret",
            audiences=["api://test"],
        )

    def test_extracts_standard_claims(self, basic_issuer: IssuerConfig) -> None:
        """Test extraction of standard JWT claims."""
        claims = {
            "sub": "user-123",
            "email": "user@example.com",
            "name": "Test User",
            "iss": "https://auth.test",
        }

        user = PrimusUser.from_claims(claims, basic_issuer)

        assert user.sub == "user-123"
        assert user.email == "user@example.com"
        assert user.name == "Test User"
        assert user.issuer == "https://auth.test"
        assert user.issuer_name == "TestAuth"

    def test_extracts_roles_from_array(self, basic_issuer: IssuerConfig) -> None:
        """Test extraction of roles from array claim."""
        claims = {
            "sub": "user-123",
            "roles": ["admin", "user", "moderator"],
        }

        user = PrimusUser.from_claims(claims, basic_issuer)

        assert user.roles == ["admin", "user", "moderator"]

    def test_extracts_roles_from_string(self, basic_issuer: IssuerConfig) -> None:
        """Test extraction of roles from string claim."""
        claims = {
            "sub": "user-123",
            "roles": "admin",
        }

        user = PrimusUser.from_claims(claims, basic_issuer)

        assert user.roles == ["admin"]

    def test_extracts_permissions(self, basic_issuer: IssuerConfig) -> None:
        """Test extraction of permissions."""
        claims = {
            "sub": "user-123",
            "permissions": ["read:users", "write:users"],
        }

        user = PrimusUser.from_claims(claims, basic_issuer)

        assert user.permissions == ["read:users", "write:users"]

    def test_uses_custom_role_claim_name(self) -> None:
        """Test using a custom role claim name."""
        issuer = IssuerConfig(
            name="TestAuth",
            type="jwt",
            issuer="https://auth.test",
            secret="secret",
            audiences=["api://test"],
            role_claim_name="custom_roles",
        )

        claims = {
            "sub": "user-123",
            "custom_roles": ["admin", "user"],
        }

        user = PrimusUser.from_claims(claims, issuer)

        assert user.roles == ["admin", "user"]

    def test_applies_claim_mappings(self) -> None:
        """Test claim mappings from provider-specific to standard claims."""
        issuer = IssuerConfig(
            name="Auth0",
            type="jwt",
            issuer="https://auth0.test",
            secret="secret",
            audiences=["api://test"],
            claim_mappings={
                "https://myapp.com/roles": "roles",
                "https://myapp.com/org": "org_id",
            },
        )

        claims = {
            "sub": "auth0|123",
            "https://myapp.com/roles": ["admin"],
            "https://myapp.com/org": "org-456",
        }

        user = PrimusUser.from_claims(claims, issuer)

        assert user.roles == ["admin"]
        assert user.organization_id == "org-456"

    def test_extracts_email_from_preferred_username(
        self, basic_issuer: IssuerConfig
    ) -> None:
        """Test fallback to preferred_username for email."""
        claims = {
            "sub": "user-123",
            "preferred_username": "user@example.com",
        }

        user = PrimusUser.from_claims(claims, basic_issuer)

        assert user.email == "user@example.com"


class TestPrimusUserRoleHelpers:
    """Tests for PrimusUser role helper methods."""

    @pytest.fixture
    def user_with_roles(self) -> PrimusUser:
        return PrimusUser(
            sub="user-123",
            roles=["admin", "user", "moderator"],
        )

    def test_has_role(self, user_with_roles: PrimusUser) -> None:
        """Test has_role method."""
        assert user_with_roles.has_role("admin") is True
        assert user_with_roles.has_role("user") is True
        assert user_with_roles.has_role("superuser") is False

    def test_has_any_role(self, user_with_roles: PrimusUser) -> None:
        """Test has_any_role method."""
        assert user_with_roles.has_any_role("admin", "superuser") is True
        assert user_with_roles.has_any_role("superuser", "guest") is False

    def test_has_all_roles(self, user_with_roles: PrimusUser) -> None:
        """Test has_all_roles method."""
        assert user_with_roles.has_all_roles("admin", "user") is True
        assert user_with_roles.has_all_roles("admin", "superuser") is False


class TestPrimusUserPermissionHelpers:
    """Tests for PrimusUser permission helper methods."""

    @pytest.fixture
    def user_with_permissions(self) -> PrimusUser:
        return PrimusUser(
            sub="user-123",
            permissions=["read:users", "write:users", "delete:posts"],
        )

    def test_has_permission(self, user_with_permissions: PrimusUser) -> None:
        """Test has_permission method."""
        assert user_with_permissions.has_permission("read:users") is True
        assert user_with_permissions.has_permission("delete:users") is False


class TestPrimusUserScopeHelpers:
    """Tests for PrimusUser scope helper methods."""

    def test_has_scope_from_scp_claim(self) -> None:
        """Test has_scope with scp claim."""
        user = PrimusUser(
            sub="user-123",
            claims={"scp": "openid profile email"},
        )

        assert user.has_scope("openid") is True
        assert user.has_scope("profile") is True
        assert user.has_scope("offline_access") is False

    def test_has_scope_from_scope_claim(self) -> None:
        """Test has_scope with scope claim."""
        user = PrimusUser(
            sub="user-123",
            claims={"scope": "read:data write:data"},
        )

        assert user.has_scope("read:data") is True
        assert user.has_scope("delete:data") is False
