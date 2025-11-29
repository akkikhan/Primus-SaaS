using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PrimusSaaS.Notifications.Services;

internal sealed class TemplateValidationHostedService : IHostedService
{
    private readonly FileTemplateService _templates;
    private readonly ILogger<TemplateValidationHostedService> _logger;

    public TemplateValidationHostedService(FileTemplateService templates, ILogger<TemplateValidationHostedService> logger)
    {
        _templates = templates;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Primus.Notifications: Validating templates under {BasePath}", _templates.BasePath);
        await _templates.ValidateAllAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
