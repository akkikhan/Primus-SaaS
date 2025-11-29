using System;
using System.IO;
using System.Threading.Tasks;
using Primus.Notifications.Services;
using Xunit;

namespace Primus.Notifications.Tests;

public class FileTemplateServiceTests
{
    [Fact]
    public async Task RenderAsync_RendersLiquidTemplateFromDisk()
    {
        var basePath = Path.Combine(Path.GetTempPath(), $"templates-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(basePath, "Welcome"));

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(basePath, "Welcome", "EmailBody.liquid"),
                "<h1>Hello {{ Name }}</h1><p>Dashboard: {{ DashboardUrl }}</p>");

            var service = new FileTemplateService(basePath);

            var output = await service.RenderAsync(
                "Welcome",
                "EmailBody",
                new { Name = "Ada", DashboardUrl = "https://app.primus" });

            Assert.Contains("Hello Ada", output);
            Assert.Contains("https://app.primus", output);
        }
        finally
        {
            if (Directory.Exists(basePath))
            {
                Directory.Delete(basePath, true);
            }
        }
    }

    [Fact]
    public async Task RenderAsync_ReturnsEmptyString_WhenTemplateMissing()
    {
        var basePath = Path.Combine(Path.GetTempPath(), $"templates-{Guid.NewGuid():N}");
        Directory.CreateDirectory(basePath);

        try
        {
            var service = new FileTemplateService(basePath);
            var output = await service.RenderAsync("Unknown", "EmailBody", new { });

            Assert.Equal(string.Empty, output);
        }
        finally
        {
            if (Directory.Exists(basePath))
            {
                Directory.Delete(basePath, true);
            }
        }
    }
}
