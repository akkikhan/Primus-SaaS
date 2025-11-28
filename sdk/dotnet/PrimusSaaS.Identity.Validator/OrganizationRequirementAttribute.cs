using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace PrimusSaaS.Identity.Validator;

/// <summary>
/// Requires the presence of an organization claim (and optionally a specific value).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequireOrganizationAttribute : Attribute, IAuthorizationFilter
{
    private readonly string? _requiredOrganization;
    private readonly string _claimType;

    public RequireOrganizationAttribute(string? requiredOrganization = null, string claimType = PrimusClaimTypes.Organization)
    {
        _requiredOrganization = requiredOrganization;
        _claimType = string.IsNullOrWhiteSpace(claimType) ? PrimusClaimTypes.Organization : claimType;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var orgValues = context.HttpContext.User?.FindAll(_claimType).Select(c => c.Value).ToList() ?? new List<string>();
        if (orgValues.Count == 0)
        {
            context.Result = new ForbidResult();
            return;
        }

        if (!string.IsNullOrWhiteSpace(_requiredOrganization) &&
            !orgValues.Contains(_requiredOrganization, StringComparer.Ordinal))
        {
            context.Result = new ForbidResult();
        }
    }
}
