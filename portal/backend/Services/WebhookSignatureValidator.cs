using System.Security.Cryptography;
using System.Text;

namespace PrimusSaaS.Portal.Api.Services;

/// <summary>
/// Validates webhook signatures from package registries (npm, NuGet)
/// </summary>
public interface IWebhookSignatureValidator
{
    bool ValidateNpmSignature(string payload, string signature, string secret);
}

public class WebhookSignatureValidator : IWebhookSignatureValidator
{
    /// <summary>
    /// Validates npm webhook signature using HMAC-SHA256
    /// npm sends signature in format: "sha256=<hash>"
    /// </summary>
    public bool ValidateNpmSignature(string payload, string signature, string secret)
    {
        if (string.IsNullOrWhiteSpace(payload) || string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(secret))
        {
            return false;
        }

        // Extract hash from "sha256=<hash>" format
        if (!signature.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var receivedHash = signature.Substring(7); // Remove "sha256=" prefix

        // Compute expected hash
        var expectedHash = ComputeHmacSha256(payload, secret);

        // Constant-time comparison to prevent timing attacks
        return SecureCompare(receivedHash, expectedHash);
    }

    private string ComputeHmacSha256(string payload, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(payloadBytes);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    private bool SecureCompare(string a, string b)
    {
        if (a.Length != b.Length)
        {
            return false;
        }

        var result = 0;
        for (int i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }

        return result == 0;
    }
}
