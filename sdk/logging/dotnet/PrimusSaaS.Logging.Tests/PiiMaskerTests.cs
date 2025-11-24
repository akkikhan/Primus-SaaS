using PrimusSaaS.Logging.Core;
using Xunit;

namespace PrimusSaaS.Logging.Tests;

public class PiiMaskerTests
{
    [Fact]
    public void MaskMessage_ShouldMaskEmails()
    {
        // Arrange
        var masker = new PiiMasker(new PiiOptions { MaskEmails = true });
        var message = "Contact me at john.doe@example.com for details.";

        // Act
        var result = masker.MaskMessage(message);

        // Assert
        Assert.DoesNotContain("john.doe@example.com", result);
        Assert.Contains("***REDACTED***", result);
    }

    [Fact]
    public void MaskContext_ShouldMaskSensitiveKeys()
    {
        // Arrange
        var masker = new PiiMasker(new PiiOptions 
        { 
            MaskEmails = true,
            CustomSensitiveKeys = new List<string> { "password", "token" }
        });

        var context = new Dictionary<string, object>
        {
            ["userId"] = "123",
            ["email"] = "john@example.com",
            ["password"] = "secret123",
            ["token"] = "abc-def",
            ["safe"] = "value"
        };

        // Act
        var result = masker.MaskContext(context);

        // Assert
        Assert.Equal("123", result["userId"]);
        Assert.Equal("value", result["safe"]);
        Assert.Equal("***REDACTED***", result["email"]);
        Assert.Equal("***REDACTED***", result["password"]);
        Assert.Equal("***REDACTED***", result["token"]);
    }

    [Fact]
    public void MaskContext_ShouldMaskNestedDictionaries()
    {
        // Arrange
        var masker = new PiiMasker(new PiiOptions { MaskEmails = true });
        var nested = new Dictionary<string, object>
        {
            ["email"] = "nested@example.com",
            ["other"] = "value"
        };
        var context = new Dictionary<string, object>
        {
            ["user"] = nested
        };

        // Act
        var result = masker.MaskContext(context);

        // Assert
        var maskedNested = result["user"] as Dictionary<string, object>;
        Assert.NotNull(maskedNested);
        Assert.Equal("***REDACTED***", maskedNested["email"]);
        Assert.Equal("value", maskedNested["other"]);
    }

    [Fact]
    public void MaskMessage_ShouldMaskCreditCards()
    {
        // Arrange
        var masker = new PiiMasker(new PiiOptions { MaskCreditCards = true });
        var message = "Payment with card 4111 1111 1111 1111 was successful.";

        // Act
        var result = masker.MaskMessage(message);

        // Assert
        Assert.DoesNotContain("4111 1111 1111 1111", result);
        Assert.Contains("***REDACTED***", result);
    }
}
