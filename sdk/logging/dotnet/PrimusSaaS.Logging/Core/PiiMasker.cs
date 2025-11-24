using System.Text.RegularExpressions;

namespace PrimusSaaS.Logging.Core;

/// <summary>
/// Handles redaction of Personally Identifiable Information (PII)
/// </summary>
public class PiiMasker
{
    private readonly Dictionary<string, Regex> _patterns;
    private readonly HashSet<string> _sensitiveKeys;

    public PiiMasker(PiiOptions options)
    {
        _patterns = new Dictionary<string, Regex>();
        _sensitiveKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Add default patterns if enabled
        if (options.MaskEmails)
        {
            _patterns["email"] = new Regex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled);
            _sensitiveKeys.Add("email");
            _sensitiveKeys.Add("userEmail");
        }

        if (options.MaskCreditCards)
        {
            _patterns["creditCard"] = new Regex(@"\b(?:\d[ -]*?){13,16}\b", RegexOptions.Compiled);
            _sensitiveKeys.Add("cardNumber");
            _sensitiveKeys.Add("creditCard");
            _sensitiveKeys.Add("cc");
        }

        if (options.MaskSSN)
        {
            _patterns["ssn"] = new Regex(@"\b\d{3}-\d{2}-\d{4}\b", RegexOptions.Compiled);
            _sensitiveKeys.Add("ssn");
            _sensitiveKeys.Add("socialSecurityNumber");
        }

        // Add custom keys
        foreach (var key in options.CustomSensitiveKeys)
        {
            _sensitiveKeys.Add(key);
        }
    }

    /// <summary>
    /// Masks PII in a string message
    /// </summary>
    public string MaskMessage(string message)
    {
        if (string.IsNullOrEmpty(message)) return message;

        var result = message;
        foreach (var pattern in _patterns.Values)
        {
            result = pattern.Replace(result, "***REDACTED***");
        }
        return result;
    }

    /// <summary>
    /// Masks PII in a context dictionary (recursive)
    /// </summary>
    public Dictionary<string, object?> MaskContext(Dictionary<string, object?> context)
    {
        if (context == null) return new Dictionary<string, object?>();

        var maskedContext = new Dictionary<string, object?>();

        foreach (var kvp in context)
        {
            if (_sensitiveKeys.Contains(kvp.Key))
            {
                maskedContext[kvp.Key] = "***REDACTED***";
            }
            else if (kvp.Value is Dictionary<string, object?> nestedDict)
            {
                maskedContext[kvp.Key] = MaskContext(nestedDict);
            }
            else if (kvp.Value is string strValue)
            {
                // Also check values for patterns even if key isn't sensitive
                maskedContext[kvp.Key] = MaskMessage(strValue);
            }
            else
            {
                maskedContext[kvp.Key] = kvp.Value;
            }
        }

        return maskedContext;
    }
}

/// <summary>
/// Configuration options for PII masking
/// </summary>
public class PiiOptions
{
    public bool MaskEmails { get; set; } = true;
    public bool MaskCreditCards { get; set; } = true;
    public bool MaskSSN { get; set; } = true;
    public List<string> CustomSensitiveKeys { get; set; } = new();
}
