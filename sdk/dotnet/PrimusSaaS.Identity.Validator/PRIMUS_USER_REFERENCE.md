# PrimusUser Reference

## Overview

The `PrimusUser` object is the core representation of an authenticated user in the PrimusSaaS Identity system. It provides a normalized view of user identity, regardless of the underlying authentication provider (Azure AD, Auth0, Local JWT, etc.).

This guide explains the properties of `PrimusUser`, how claims are mapped, and how to extend it.

---

## Properties

| Property | Type | Description | Source Claim (Standard) |
|----------|------|-------------|-------------------------|
| `UserId` | `string` | Unique identifier for the user. | `sub`, `oid`, or `uid` |
| `Email` | `string` | User's email address. | `email`, `upn`, or `unique_name` |
| `Name` | `string` | User's display name. | `name`, `given_name`, or `nickname` |
| `Roles` | `List<string>` | List of roles assigned to the user. | `roles`, `role`, or `groups` |
| `AdditionalClaims` | `Dictionary<string, string>` | Any other claims not mapped to standard properties. | All other claims |

---

## Usage

### 1. Accessing via Extension Method (Recommended)

The easiest way to access the current user is using the `GetPrimusUser()` extension method on `HttpContext`.

```csharp
using PrimusSaaS.Identity.Validator;

[HttpGet("me")]
public IActionResult GetMe()
{
    // Get the current user from HttpContext
    var user = HttpContext.GetPrimusUser();
    
    if (user == null)
    {
        return Unauthorized();
    }
    
    return Ok(new 
    {
        id = user.UserId,
        email = user.Email,
        roles = user.Roles
    });
}
```

### 2. Accessing via ClaimsPrincipal

You can also manually create a `PrimusUser` from any `ClaimsPrincipal`.

```csharp
var user = PrimusUser.FromClaimsPrincipal(User);
```

### 3. Accessing Additional Claims

Any claims that don't map to the standard properties are available in `AdditionalClaims`.

```csharp
var tenantId = user.AdditionalClaims.ContainsKey("tid") 
    ? user.AdditionalClaims["tid"] 
    : "default";

var department = user.AdditionalClaims.GetValueOrDefault("department", "Unknown");
```

---

## Claim Mapping Logic

When `PrimusUser` is created, it attempts to map claims from the token using the following priority:

### UserId Mapping
1. `ClaimTypes.NameIdentifier` (`http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier`)
2. `sub` (Subject)
3. `oid` (Object ID - Azure AD)

### Email Mapping
1. `ClaimTypes.Email` (`http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress`)
2. `email`
3. `upn` (User Principal Name)

### Name Mapping
1. `ClaimTypes.Name` (`http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name`)
2. `name`
3. `given_name`

### Roles Mapping
1. `ClaimTypes.Role` (`http://schemas.microsoft.com/ws/2008/06/identity/claims/role`)
2. `roles`
3. `role`

---

## Extending PrimusUser

Since `PrimusUser` is a simple POCO, you cannot directly add properties to it without modifying the source. However, you can use extension methods to provide typed access to custom claims.

### Example: Adding Tenant ID Support

```csharp
public static class PrimusUserExtensions
{
    public static string GetTenantId(this PrimusUser user)
    {
        if (user.AdditionalClaims.TryGetValue("tid", out var tid))
        {
            return tid;
        }
        return "default";
    }
    
    public static bool IsAdmin(this PrimusUser user)
    {
        return user.Roles.Contains("Admin") || user.Roles.Contains("SuperAdmin");
    }
}

// Usage
var tenantId = user.GetTenantId();
if (user.IsAdmin()) { /* ... */ }
```

---

## Serialization

`PrimusUser` is designed to be JSON-serializable. This is useful if you need to pass the user context to background jobs or other services.

```csharp
var json = System.Text.Json.JsonSerializer.Serialize(user);
```

**Note:** The `AdditionalClaims` dictionary only stores string values. Complex claim types (arrays/objects) in the original token may be flattened or serialized as JSON strings depending on the JWT handler.
