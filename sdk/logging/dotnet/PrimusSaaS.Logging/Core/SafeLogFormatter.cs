using System.Buffers;
using System.Text;
using System.Text.Json;

namespace PrimusSaaS.Logging.Core;

/// <summary>
/// Formats log entries using the safe serializer and enforces size limits.
/// </summary>
internal class SafeLogFormatter
{
    private readonly SafeObjectSerializer _serializer;
    private readonly SerializationOptions _options;

    public SafeLogFormatter(SafeObjectSerializer serializer)
    {
        _serializer = serializer;
        _options = serializer.Options;
    }

    public string Format(LogEntry entry)
    {
        var payload = new Dictionary<string, object?>
        {
            ["timestamp"] = entry.Timestamp.ToString("O"),
            ["level"] = entry.Level.ToString().ToUpperInvariant(),
            ["message"] = entry.Message,
            ["context"] = entry.Context ?? new Dictionary<string, object?>()
        };

        try
        {
            var json = SerializeWithPooling(payload);
            if (json.Length > _options.MaxContextBytes)
            {
                payload["context"] = new Dictionary<string, object?>
                {
                    ["_truncated"] = true,
                    ["_reason"] = "context exceeded configured limit",
                    ["_keys"] = entry.Context?.Keys?.ToArray() ?? Array.Empty<string>()
                };

                json = SerializeWithPooling(payload);
            }

            return Encoding.UTF8.GetString(json);
        }
        catch (Exception ex)
        {
            var fallback = new Dictionary<string, object?>
            {
                ["timestamp"] = entry.Timestamp.ToString("O"),
                ["level"] = entry.Level.ToString().ToUpperInvariant(),
                ["message"] = entry.Message,
                ["context"] = $"serialization_failed: {ex.Message}"
            };

            var bytes = SerializeWithPooling(fallback);
            return Encoding.UTF8.GetString(bytes);
        }
    }

    private byte[] SerializeWithPooling(object payload)
    {
        var buffer = new ArrayBufferWriter<byte>(256);
        using (var writer = new Utf8JsonWriter(buffer))
        {
            JsonSerializer.Serialize(writer, payload, _serializer.JsonOptions);
        }
        return buffer.WrittenSpan.ToArray();
    }
}
