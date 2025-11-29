using System;
using System.Collections.Generic;
using PrimusSaaS.Logging.Core;
using Xunit;

namespace PrimusSaaS.Logging.Tests;

public class LoggerTests
{
    private static Logger CreateLogger() => new Logger(new LoggerOptions
    {
        ApplicationId = "test",
        Environment = "test",
        Targets = new List<TargetConfig> { new() { Type = "console" } }
    });

    [Fact]
    public void Info_AllowsAnonymousObjectContext()
    {
        var logger = CreateLogger();
        logger.Info("Order created", new { orderId = 1, total = 9.99 });
    }

    [Fact]
    public void Error_WithException_AllowsAnonymousObjectContext()
    {
        var logger = CreateLogger();
        logger.Error(new InvalidOperationException("boom"), "Payment failed", new { orderId = 1, reason = "declined" });
    }
}
