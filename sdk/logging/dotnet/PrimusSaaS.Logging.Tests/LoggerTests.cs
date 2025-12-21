using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using PrimusSaaS.Logging.Core;
using PrimusSaaS.Logging.Targets;
using Xunit;
using PrimusLogLevel = PrimusSaaS.Logging.Core.LogLevel;

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

    [Fact]
    public void Sampling_DoesNotDropErrors_WhenAlwaysLogOnErrorTrue()
    {
        var target = new TestTarget();
        var logger = new Logger(new LoggerOptions
        {
            ApplicationId = "test",
            Environment = "test",
            Sampling = new SamplingOptions { Enabled = true, SampleRate = 0.1, AlwaysLogOnError = true },
            CustomTargets = new List<ITarget> { target }
        });

        logger.Info("should be sampled out");
        logger.Error("should always log");

        Assert.Single(target.Logs);
        Assert.Equal(PrimusLogLevel.Error, target.Logs[0].Level);
    }

    [Fact]
    public void Category_Truncation_IsConfigurable()
    {
        var target = new TestTarget();
        var logger = new Logger(new LoggerOptions
        {
            ApplicationId = "test",
            Environment = "test",
            TruncateCategoryNames = true,
            MaxCategoryLength = 10,
            CustomTargets = new List<ITarget> { target }
        });

        var adapter = new Providers.PrimusLoggerAdapter("Very.Long.Category.Name.For.Component", logger);
        adapter.LogInformation("hello");

        var context = target.Logs[0].Context;
        Assert.Equal("Very.Long....", context["category"]);
        Assert.Equal("Very.Long.Category.Name.For.Component", context["categoryFull"]);
    }
}
