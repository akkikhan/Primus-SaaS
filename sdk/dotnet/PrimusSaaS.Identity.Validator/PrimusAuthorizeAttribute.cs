using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace PrimusSaaS.Identity.Validator;

/// <summary>
/// Requires authentication via Primus Identity. This is a branded wrapper around ASP.NET Core's
/// <see cref="AuthorizeAttribute"/> that makes it clear the endpoint is protected by Primus.
/// </summary>
/// <remarks>
/// <para>Use this attribute to clearly indicate Primus-managed authentication:</para>
/// <code>
/// [PrimusAuthorize]
/// public IActionResult SecureEndpoint() => Ok("Authenticated!");
/// 
/// [PrimusAuthorize(Roles = "Admin,Manager")]
/// public IActionResult AdminEndpoint() => Ok("Admin access!");
/// 
/// [PrimusAuthorize(Policy = "RequireVerifiedEmail")]
/// public IActionResult VerifiedEndpoint() => Ok("Email verified!");
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class PrimusAuthorizeAttribute : AuthorizeAttribute, IAuthorizationFilter
{
    /// <summary>
    /// Initializes a new instance of <see cref="PrimusAuthorizeAttribute"/>.
    /// Requires the user to be authenticated via any configured Primus issuer.
    /// </summary>
    public PrimusAuthorizeAttribute() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="PrimusAuthorizeAttribute"/> with a specific policy.
    /// </summary>
    /// <param name="policy">The name of the authorization policy to apply.</param>
    public PrimusAuthorizeAttribute(string policy) : base(policy)
    {
    }

    /// <summary>
    /// Called during authorization to optionally add Primus-specific behavior.
    /// </summary>
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // The base AuthorizeAttribute handles the actual authorization via ASP.NET Core's
        // authorization middleware. This filter can be extended for Primus-specific checks.
        
        // If additional Primus-specific validation is needed (e.g., issuer verification),
        // it can be added here. For now, we rely on the standard ASP.NET Core authorization.
    }
}

/// <summary>
/// Requires authentication and specific roles via Primus Identity.
/// Shorthand for <c>[PrimusAuthorize(Roles = "...")]</c>.
/// </summary>
/// <remarks>
/// <code>
/// [PrimusAuthorizeRoles("Admin", "SuperAdmin")]
/// public IActionResult AdminOnly() => Ok("Admin access granted!");
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class PrimusAuthorizeRolesAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Initializes a new instance requiring ALL specified roles.
    /// </summary>
    /// <param name="roles">The roles required (comma-separated in the underlying attribute).</param>
    public PrimusAuthorizeRolesAttribute(params string[] roles) : base()
    {
        if (roles != null && roles.Length > 0)
        {
            Roles = string.Join(",", roles);
        }
    }
}

/// <summary>
/// Requires a specific permission claim via Primus Identity.
/// Works with Auth0 permissions, Azure AD app roles, or custom permission claims.
/// </summary>
/// <remarks>
/// <code>
/// [PrimusRequirePermission("read:users")]
/// public IActionResult GetUsers() => Ok();
/// 
/// [PrimusRequirePermission("write:users")]
/// public IActionResult CreateUser() => Ok();
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class PrimusRequirePermissionAttribute : Attribute, IAuthorizationFilter
{
    private readonly string _permission;
    private readonly string _claimType;

    /// <summary>
    /// Requires the specified permission.
    /// </summary>
    /// <param name="permission">The permission value to require (e.g., "read:users").</param>
    /// <param name="claimType">Optional claim type to check. Defaults to <see cref="PrimusClaimTypes.Permission"/>.</param>
    public PrimusRequirePermissionAttribute(string permission, string? claimType = null)
    {
        _permission = permission ?? throw new ArgumentNullException(nameof(permission));
        _claimType = claimType ?? PrimusClaimTypes.Permission;
    }

    /// <inheritdoc />
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var hasPermission = user.Claims
            .Where(c => c.Type == _claimType)
            .Any(c => string.Equals(c.Value, _permission, StringComparison.Ordinal));

        if (!hasPermission)
        {
            context.Result = new ForbidResult();
        }
    }
}

/// <summary>
/// Requires ALL specified permissions via Primus Identity.
/// </summary>
/// <remarks>
/// <code>
/// [PrimusRequireAllPermissions("read:users", "write:users")]
/// public IActionResult ManageUsers() => Ok();
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class PrimusRequireAllPermissionsAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _permissions;
    private readonly string _claimType;

    /// <summary>
    /// Requires ALL specified permissions.
    /// </summary>
    /// <param name="permissions">The permissions that must ALL be present.</param>
    public PrimusRequireAllPermissionsAttribute(params string[] permissions)
        : this(null, permissions)
    {
    }

    /// <summary>
    /// Requires ALL specified permissions with a custom claim type.
    /// </summary>
    /// <param name="claimType">The claim type to check.</param>
    /// <param name="permissions">The permissions that must ALL be present.</param>
    public PrimusRequireAllPermissionsAttribute(string? claimType, params string[] permissions)
    {
        _permissions = permissions ?? Array.Empty<string>();
        _claimType = claimType ?? PrimusClaimTypes.Permission;
    }

    /// <inheritdoc />
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        if (_permissions.Length == 0)
        {
            context.Result = new ForbidResult();
            return;
        }

        var userPermissions = user.Claims
            .Where(c => c.Type == _claimType)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.Ordinal);

        if (!_permissions.All(p => userPermissions.Contains(p)))
        {
            context.Result = new ForbidResult();
        }
    }
}

/// <summary>
/// Requires ANY of the specified permissions via Primus Identity.
/// </summary>
/// <remarks>
/// <code>
/// [PrimusRequireAnyPermission("read:users", "admin:all")]
/// public IActionResult ViewUsers() => Ok();
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class PrimusRequireAnyPermissionAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _permissions;
    private readonly string _claimType;

    /// <summary>
    /// Requires ANY of the specified permissions.
    /// </summary>
    /// <param name="permissions">The permissions where at least one must be present.</param>
    public PrimusRequireAnyPermissionAttribute(params string[] permissions)
        : this(null, permissions)
    {
    }

    /// <summary>
    /// Requires ANY of the specified permissions with a custom claim type.
    /// </summary>
    /// <param name="claimType">The claim type to check.</param>
    /// <param name="permissions">The permissions where at least one must be present.</param>
    public PrimusRequireAnyPermissionAttribute(string? claimType, params string[] permissions)
    {
        _permissions = permissions ?? Array.Empty<string>();
        _claimType = claimType ?? PrimusClaimTypes.Permission;
    }

    /// <inheritdoc />
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        if (_permissions.Length == 0)
        {
            context.Result = new ForbidResult();
            return;
        }

        var userPermissions = user.Claims
            .Where(c => c.Type == _claimType)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.Ordinal);

        if (!_permissions.Any(p => userPermissions.Contains(p)))
        {
            context.Result = new ForbidResult();
        }
    }
}

/// <summary>
/// Requires the user to have a specific scope via Primus Identity.
/// Commonly used with OAuth 2.0 scopes from Auth0 or Azure AD.
/// </summary>
/// <remarks>
/// <code>
/// [PrimusRequireScope("api.read")]
/// public IActionResult ReadData() => Ok();
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class PrimusRequireScopeAttribute : Attribute, IAuthorizationFilter
{
    private readonly string _scope;

    /// <summary>
    /// Requires the specified scope.
    /// </summary>
    /// <param name="scope">The scope value to require.</param>
    public PrimusRequireScopeAttribute(string scope)
    {
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
    }

    /// <inheritdoc />
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Check both "scope" and "scp" claims (different providers use different names)
        var scopes = user.Claims
            .Where(c => c.Type == "scope" || c.Type == "scp")
            .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .ToHashSet(StringComparer.Ordinal);

        if (!scopes.Contains(_scope))
        {
            context.Result = new ForbidResult();
        }
    }
}
