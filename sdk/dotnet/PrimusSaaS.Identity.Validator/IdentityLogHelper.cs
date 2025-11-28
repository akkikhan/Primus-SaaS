using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;

namespace PrimusSaaS.Identity.Validator;

/// <summary>
/// Helper to build structured, redacted log data for token validation events.
/// </summary>
internal static class IdentityLogHelper
{
    public static Dictionary<string, object?> BuildValidationLogData(
        JwtSecurityToken? jwt,
        ClaimsPrincipal? principal,
        PrimusIdentityLoggingOptions loggingOptions)
    {
        var data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["issuer"] = jwt?.Issuer,
            ["kid"] = jwt?.Header.Kid,
            ["audiences"] = jwt?.Audiences?.ToArray()
        };

        if (!loggingOptions.RedactSensitiveData)
        {
            var sub = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal?.FindFirst("sub")?.Value;
            if (!string.IsNullOrWhiteSpace(sub))
            {
                data["sub"] = sub;
            }
        }
        else
        {
            var sub = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal?.FindFirst("sub")?.Value;
            if (!string.IsNullOrWhiteSpace(sub))
            {
                data["sub_hash"] = Hash(sub);
            }
        }

        return data;
    }

    private static string Hash(string value)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
