using System.Collections;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace PrimusSaaS.Logging.Core;

/// <summary>
/// Sanitizes arbitrary objects for safe logging.
/// </summary>
internal class SafeObjectSerializer
{
    private readonly SerializationOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    public SafeObjectSerializer(SerializationOptions options)
    {
        _options = (options ?? new SerializationOptions()).Clone();
        _options.Validate();

        _jsonOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        if (_options.IgnoreCycles)
        {
            _jsonOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        }

        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public SerializationOptions Options => _options;

    public JsonSerializerOptions JsonOptions => _jsonOptions;

    public Dictionary<string, object?> SanitizeContext(IReadOnlyDictionary<string, object?>? context)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (context == null) return result;

        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        foreach (var kvp in context)
        {
            result[kvp.Key] = SanitizeValue(kvp.Value, 0, visited);
        }

        return result;
    }

    private object? SanitizeValue(object? value, int depth, HashSet<object> visited)
    {
        if (value == null)
        {
            return null;
        }

        if (depth >= _options.MaxDepth)
        {
            return "[MaxDepth]";
        }

        if (!IsSimple(value))
        {
            if (!visited.Add(value))
            {
                return "[Cycle]";
            }
        }

        switch (value)
        {
            case string s:
                return TruncateString(s);
            case bool or byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal:
                return value;
            case DateTime dt:
                return dt;
            case DateTimeOffset dto:
                return dto;
            case Guid guid:
                return guid.ToString();
            case Enum enumValue:
                return enumValue.ToString();
            case Type type:
                return type.FullName ?? type.Name;
            case Exception ex:
                return SanitizeException(ex, depth + 1, visited);
            case ClaimsPrincipal principal:
                return SanitizeClaimsPrincipal(principal, depth + 1, visited);
            case ClaimsIdentity identity:
                return SanitizeClaimsIdentity(identity, depth + 1, visited);
            case Claim claim:
                return new Dictionary<string, object?>
                {
                    ["type"] = TruncateString(claim.Type),
                    ["value"] = TruncateString(claim.Value)
                };
            case HttpContext httpContext:
                return SanitizeHttpContext(httpContext);
            case HttpRequest request:
                return SanitizeHttpRequest(request);
            case HttpResponse response:
                return SanitizeHttpResponse(response);
            case IDictionary<string, object?> stringObjectDict:
                return SanitizeDictionary(stringObjectDict, depth + 1, visited);
            case IDictionary dict:
                return SanitizeUntypedDictionary(dict, depth + 1, visited);
            case IEnumerable enumerable when value is not string:
                return SanitizeEnumerable(enumerable, depth + 1, visited);
            default:
                return TruncateString(value.ToString() ?? value.GetType().Name);
        }
    }

    private Dictionary<string, object?> SanitizeDictionary(IDictionary<string, object?> dict, int depth, HashSet<object> visited)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in dict)
        {
            result[kvp.Key] = SanitizeValue(kvp.Value, depth, visited);
        }

        return result;
    }

    private Dictionary<string, object?> SanitizeUntypedDictionary(IDictionary dict, int depth, HashSet<object> visited)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (DictionaryEntry entry in dict)
        {
            var key = entry.Key?.ToString() ?? "null";
            result[key] = SanitizeValue(entry.Value, depth, visited);
        }

        return result;
    }

    private object SanitizeEnumerable(IEnumerable enumerable, int depth, HashSet<object> visited)
    {
        var items = new List<object?>();
        int count = 0;

        foreach (var item in enumerable)
        {
            if (count >= _options.MaxEnumerableLength)
            {
                items.Add("[Truncated]");
                break;
            }

            items.Add(SanitizeValue(item, depth, visited));
            count++;
        }

        return items;
    }

    private Dictionary<string, object?> SanitizeException(Exception ex, int depth, HashSet<object> visited)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["type"] = ex.GetType().FullName ?? ex.GetType().Name,
            ["message"] = TruncateString(ex.Message),
            ["stackTrace"] = TruncateString(ex.StackTrace ?? string.Empty)
        };

        if (ex.InnerException != null)
        {
            result["inner"] = SanitizeException(ex.InnerException, depth + 1, visited);
        }

        return result;
    }

    private Dictionary<string, object?> SanitizeClaimsPrincipal(ClaimsPrincipal principal, int depth, HashSet<object> visited)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["identities"] = SanitizeEnumerable(principal.Identities, depth, visited)
        };

        return result;
    }

    private Dictionary<string, object?> SanitizeClaimsIdentity(ClaimsIdentity identity, int depth, HashSet<object> visited)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["authenticationType"] = identity.AuthenticationType,
            ["isAuthenticated"] = identity.IsAuthenticated,
            ["name"] = identity.Name
        };

        result["claims"] = SanitizeEnumerable(identity.Claims, depth, visited);

        return result;
    }

    private Dictionary<string, object?> SanitizeHttpContext(HttpContext context)
    {
        try
        {
            // Limit to safe, non-recursive request/response slices
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["traceIdentifier"] = context.TraceIdentifier,
                ["request"] = SanitizeHttpRequest(context.Request),
                ["response"] = SanitizeHttpResponse(context.Response)
            };
        }
        catch (Exception ex)
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["error"] = TruncateString(ex.Message)
            };
        }
    }

    private Dictionary<string, object?> SanitizeHttpRequest(HttpRequest request)
    {
        var safeHeaders = new Dictionary<string, object?>();
        int count = 0;

        try
        {
            foreach (var header in request.Headers)
            {
                if (count >= _options.MaxEnumerableLength)
                {
                    safeHeaders["_truncated"] = true;
                    break;
                }

                var headerValues = header.Value.ToArray() ?? Array.Empty<string>();
                safeHeaders[header.Key] = TruncateString(string.Join(",", headerValues));
                count++;
            }
        }
        catch (Exception ex)
        {
            safeHeaders["_error"] = TruncateString(ex.Message);
        }

        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["method"] = request.Method,
            ["path"] = request.Path.ToString(),
            ["queryString"] = request.QueryString.ToString(),
            ["scheme"] = request.Scheme,
            ["host"] = request.Host.ToString(),
            ["headers"] = safeHeaders
        };
    }

    private Dictionary<string, object?> SanitizeHttpResponse(HttpResponse response)
    {
        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["statusCode"] = response.StatusCode
        };
    }

    private static bool IsSimple(object value)
    {
        return value is string
               || value is bool
               || value is byte or sbyte or short or ushort or int or uint or long or ulong
               || value is float or double or decimal
               || value is Guid
               || value is DateTime or DateTimeOffset
               || value is Enum;
    }

    private string TruncateString(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        if (value.Length <= _options.MaxStringLength)
        {
            return value;
        }

        return value[.._options.MaxStringLength] + "...";
    }
}
