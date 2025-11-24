using System.Security.Claims;
using PrimusSaaS.Logging.Core;
using Xunit;

namespace PrimusSaaS.Logging.Tests;

public class SafeSerializationTests
{
    [Fact]
    public void ToJson_ShouldHandle_SystemTypeWithoutThrowing()
    {
        var serializer = new SafeObjectSerializer(new SerializationOptions());
        var formatter = new SafeLogFormatter(serializer);
        var context = serializer.SanitizeContext(new Dictionary<string, object?>
        {
            ["type"] = typeof(string)
        });

        var entry = LogEntry.Create(LogLevel.Info, "type-test", context, formatter);
        var json = entry.ToJson();

        Assert.Contains("System.String", json);
    }

    [Fact]
    public void ToJson_ShouldHandle_ClaimsPrincipal()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("sub", "user-123"),
            new Claim("email", "user@example.com")
        }, "test-auth");
        var principal = new ClaimsPrincipal(identity);

        var serializer = new SafeObjectSerializer(new SerializationOptions());
        var formatter = new SafeLogFormatter(serializer);
        var context = serializer.SanitizeContext(new Dictionary<string, object?>
        {
            ["principal"] = principal
        });

        var entry = LogEntry.Create(LogLevel.Info, "claims-test", context, formatter);
        var json = entry.ToJson();

        Assert.Contains("claims", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("user@example.com", json);
    }

    [Fact]
    public void SanitizeContext_ShouldBreakCycles()
    {
        var serializer = new SafeObjectSerializer(new SerializationOptions());
        var source = new Dictionary<string, object?>();
        source["self"] = source;

        var sanitized = serializer.SanitizeContext(source);

        var nested = Assert.IsType<Dictionary<string, object?>>(sanitized["self"]);
        Assert.Equal("[Cycle]", nested["self"]);
    }

    [Fact]
    public void SafeLogFormatter_ShouldTruncateOversizedContext()
    {
        var serializer = new SafeObjectSerializer(new SerializationOptions
        {
            MaxContextBytes = 600,
            MaxStringLength = 10000
        });
        var formatter = new SafeLogFormatter(serializer);
        var largeString = new string('a', 5000);
        var context = serializer.SanitizeContext(new Dictionary<string, object?>
        {
            ["payload"] = largeString
        });

        var entry = LogEntry.Create(LogLevel.Info, "big-context", context, formatter);
        var json = entry.ToJson();

        Assert.Contains("\"_truncated\":true", json);
    }
}
