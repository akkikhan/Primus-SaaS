using System.Text.Json.Serialization;

namespace PrimusSaaS.Identity.Validator.Models;

/// <summary>
/// Represents a JSON Web Key from a JWKS endpoint.
/// </summary>
public class PrimusJsonWebKey
{
    /// <summary>
    /// Key type (e.g., "RSA").
    /// </summary>
    [JsonPropertyName("kty")]
    public string KeyType { get; set; } = string.Empty;

    /// <summary>
    /// Key usage (e.g., "sig" for signature).
    /// </summary>
    [JsonPropertyName("use")]
    public string? Use { get; set; }

    /// <summary>
    /// Key ID - unique identifier for this key.
    /// </summary>
    [JsonPropertyName("kid")]
    public string KeyId { get; set; } = string.Empty;

    /// <summary>
    /// X.509 certificate chain.
    /// </summary>
    [JsonPropertyName("x5c")]
    public List<string>? X509CertificateChain { get; set; }

    /// <summary>
    /// Modulus for RSA public key (Base64 URL encoded).
    /// </summary>
    [JsonPropertyName("n")]
    public string? Modulus { get; set; }

    /// <summary>
    /// Exponent for RSA public key (Base64 URL encoded).
    /// </summary>
    [JsonPropertyName("e")]
    public string? Exponent { get; set; }

    /// <summary>
    /// Algorithm (e.g., "RS256").
    /// </summary>
    [JsonPropertyName("alg")]
    public string? Algorithm { get; set; }

    /// <summary>
    /// X.509 certificate thumbprint (SHA-1).
    /// </summary>
    [JsonPropertyName("x5t")]
    public string? X509Thumbprint { get; set; }

    /// <summary>
    /// Issuer of the key.
    /// </summary>
    [JsonPropertyName("issuer")]
    public string? Issuer { get; set; }
}

/// <summary>
/// Represents a JWKS (JSON Web Key Set) document.
/// </summary>
public class PrimusJsonWebKeySet
{
    /// <summary>
    /// Array of JSON Web Keys.
    /// </summary>
    [JsonPropertyName("keys")]
    public List<PrimusJsonWebKey> Keys { get; set; } = new();
}

/// <summary>
/// Represents OpenID Connect configuration metadata.
/// </summary>
public class PrimusOpenIdConfiguration
{
    /// <summary>
    /// The issuer identifier.
    /// </summary>
    [JsonPropertyName("issuer")]
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// URL of the JWKS endpoint.
    /// </summary>
    [JsonPropertyName("jwks_uri")]
    public string JwksUri { get; set; } = string.Empty;

    /// <summary>
    /// Authorization endpoint URL.
    /// </summary>
    [JsonPropertyName("authorization_endpoint")]
    public string? AuthorizationEndpoint { get; set; }

    /// <summary>
    /// Token endpoint URL.
    /// </summary>
    [JsonPropertyName("token_endpoint")]
    public string? TokenEndpoint { get; set; }

    /// <summary>
    /// Supported signing algorithms.
    /// </summary>
    [JsonPropertyName("id_token_signing_alg_values_supported")]
    public List<string>? SigningAlgorithmsSupported { get; set; }

    /// <summary>
    /// Supported response types.
    /// </summary>
    [JsonPropertyName("response_types_supported")]
    public List<string>? ResponseTypesSupported { get; set; }

    /// <summary>
    /// Supported subject types.
    /// </summary>
    [JsonPropertyName("subject_types_supported")]
    public List<string>? SubjectTypesSupported { get; set; }
}
