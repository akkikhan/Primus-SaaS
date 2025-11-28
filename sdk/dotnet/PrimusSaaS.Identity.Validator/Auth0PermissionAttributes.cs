using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace PrimusSaaS.Identity.Validator;

/// <summary>
/// Requires a single Auth0 permission (claim type: permissions).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class Auth0PermissionAttribute : Attribute, IAuthorizationFilter
{
    private readonly string _permission;

    public Auth0PermissionAttribute(string permission)
    {
        _permission = permission ?? throw new ArgumentNullException(nameof(permission));
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (!HasPermission(context.HttpContext.User, _permission))
        {
            context.Result = new ForbidResult();
        }
    }

    private static bool HasPermission(ClaimsPrincipal user, string permission)
    {
        return user?.Claims
            .Where(c => c.Type == PrimusClaimTypes.Permission)
            .Select(c => c.Value)
            .Contains(permission, StringComparer.Ordinal) == true;
    }
}

/// <summary>
/// Requires ALL listed Auth0 permissions.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class Auth0PermissionsAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _permissions;

    public Auth0PermissionsAttribute(params string[] permissions)
    {
        _permissions = permissions ?? Array.Empty<string>();
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (_permissions.Length == 0)
        {
            context.Result = new ForbidResult();
            return;
        }

        var userPermissions = context.HttpContext.User?.Claims
            .Where(c => c.Type == PrimusClaimTypes.Permission)
            .Select(c => c.Value)
            .ToList() ?? new List<string>();

        if (!_permissions.All(req => userPermissions.Contains(req, StringComparer.Ordinal)))
        {
            context.Result = new ForbidResult();
        }
    }
}

/// <summary>
/// Requires ANY of the listed Auth0 permissions.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class Auth0AnyPermissionAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _permissions;

    public Auth0AnyPermissionAttribute(params string[] permissions)
    {
        _permissions = permissions ?? Array.Empty<string>();
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (_permissions.Length == 0)
        {
            context.Result = new ForbidResult();
            return;
        }

        var userPermissions = context.HttpContext.User?.Claims
            .Where(c => c.Type == PrimusClaimTypes.Permission)
            .Select(c => c.Value)
            .ToList() ?? new List<string>();

        if (!_permissions.Any(req => userPermissions.Contains(req, StringComparer.Ordinal)))
        {
            context.Result = new ForbidResult();
        }
    }
}
