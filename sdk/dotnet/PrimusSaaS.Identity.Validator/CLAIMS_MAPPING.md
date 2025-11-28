# Claims Mapping & Reference

This guide details the claims expected by **PrimusSaaS.Identity.Validator** and how they are mapped to the `PrimusUser` object.

## Required Claims

The validator expects these standard claims to function correctly.

| Claim Type | Standard Name | Description | Required? |
|------------|---------------|-------------|-----------|
| `iss` | Issuer | The entity that issued the token (must match config). | ✅ **Yes** |
| `aud` | Audience | The intended recipient (must match config). | ✅ **Yes** |
| `exp` | Expiration | Timestamp when token expires. | ✅ **Yes** |
| `sub` | Subject | Unique identifier for the user. | ✅ **Yes** |

## Recommended Claims

These claims populate the `PrimusUser` convenience object properties.

| Claim Type | PrimusUser Property | Description |
|------------|---------------------|-------------|
| `email` | `Email` | User's email address. |
| `name` | `Name` | User's full name. |
| `roles` | `Roles` | List of user roles (array). |
| `tid` | `TenantId` | Tenant identifier (for multi-tenant apps). |

### Example Token Payload

```json
{
  "iss": "https://localhost:5265",
  "aud": "api://my-app",
  "sub": "user-123",
  "exp": 1735689600,
  "email": "alice@example.com",
  "name": "Alice Smith",
  "roles": ["Admin", "User"],
  "tid": "tenant-abc"
}
```

## Accessing Claims in Code

### Using `GetPrimusUser()` (Recommended)

The extension method automatically maps standard claims to a strongly-typed object.

```csharp
var user = HttpContext.GetPrimusUser();

// Mapped properties
string id = user.UserId;      // maps from 'sub' or 'nameidentifier'
string email = user.Email;    // maps from 'email'
string name = user.Name;      // maps from 'name'
List<string> roles = user.Roles; // maps from 'roles' or 'role'
```

### Accessing Raw Claims

You can always access the underlying claims collection if you have custom claims.

```csharp
// Access any custom claim
var department = User.FindFirst("department")?.Value;
var subscriptionLevel = User.FindFirst("subscription_level")?.Value;
```

## Custom Claim Mapping

If your identity provider uses different claim names, you can map them manually or use the `TenantResolver` option.

### Using TenantResolver

```csharp
builder.Services.AddPrimusIdentity(options =>
{
    options.TenantResolver = claims => new TenantContext
    {
        // Map custom 'org_id' claim to TenantId
        TenantId = claims.Get("org_id") ?? "default",
        
        // Map custom 'groups' claim to Roles
        Roles = claims.Get<List<string>>("groups")
    };
});
```

## Azure AD Specifics

Azure AD tokens often have specific claim formats.

- **Roles**: Azure AD sends roles in the `roles` claim (if defined in App Manifest) or `groups` (if configured).
- **Name**: Often in `name` or `unique_name`.
- **Email**: Often in `email` or `upn`.

The `GetPrimusUser()` helper attempts to find the best match from common variations.

## Auth0 Defaults

- **Subject**: `sub` (e.g., `auth0|user-id` or `google-oauth2|id`) → mapped to `ClaimTypes.NameIdentifier` automatically.
- **Email/Name**: `email`, `name` → mapped automatically.
- **Permissions**: `permissions` (array) → mirrored to `PrimusClaimTypes.Permission` for policy checks.
- **Organization**: `org_id` (or your configured `OrganizationClaimName`) → mirrored to `PrimusClaimTypes.Organization` when present.
- **Roles (optional)**: If you set `RoleClaimName` (e.g., a namespaced `https://api-id/roles`), values are mirrored into `ClaimTypes.Role` so `[Authorize(Roles = "...")]` works.
- **Social detection**: `sub` prefix (e.g., `google-oauth2|...`) → `PrimusUser.IdentityProvider` + `IsSocialLogin`.
- **Machine-to-machine**: `sub` ending with `@clients` or `gty=client-credentials` → `PrimusUser.IsMachineToMachine` + `ClientId` (from `sub`/`azp`).

Namespaced claims are supported—set the exact claim name in `RoleClaimName`, `PermissionClaimName`, or `OrganizationClaimName` to normalize them.
