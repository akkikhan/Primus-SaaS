using System;
using System.IO;
using System.Threading.Tasks;
using Fluid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PrimusSaaS.Notifications.Abstractions;

namespace PrimusSaaS.Notifications.Services;

public class FileTemplateService : ITemplateService
{
    private readonly string _basePath;
    private readonly FluidParser _parser;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, IFluidTemplate> _cache;
    private readonly ILogger<FileTemplateService> _logger;

    public FileTemplateService(string basePath, ILogger<FileTemplateService>? logger = null)
    {
        _basePath = basePath;
        _parser = new FluidParser();
        _cache = new System.Collections.Concurrent.ConcurrentDictionary<string, IFluidTemplate>();
        _logger = logger ?? NullLogger<FileTemplateService>.Instance;
    }

    public async Task<string> RenderAsync(string notificationType, string channel, object model)
    {
        // Convention: Templates/{NotificationType}/{Channel}.liquid
        var fileName = $"{channel}.liquid";
        var path = Path.Combine(_basePath, notificationType, fileName);
        var cacheKey = path;

        if (!_cache.TryGetValue(cacheKey, out var template))
        {
            if (!File.Exists(path))
            {
                _logger.LogWarning("Template not found at path {TemplatePath}", path);
                return string.Empty;
            }

            var source = await File.ReadAllTextAsync(path);
            if (!_parser.TryParse(source, out template, out var error))
            {
                _logger.LogError("Failed to parse template {TemplatePath}: {Error}", path, error);
                throw new Exception($"Failed to parse template {path}: {error}");
            }
            
            _cache.TryAdd(cacheKey, template);
        }

        var effectiveModel = model ?? new { };
        var options = new TemplateOptions();
        options.MemberAccessStrategy.Register(effectiveModel.GetType());
        var context = new TemplateContext(effectiveModel, options);
        return await template.RenderAsync(context);
    }
}
