using System;
using System.IO;
using System.Threading.Tasks;
using Fluid;
using Primus.Notifications.Abstractions;

namespace Primus.Notifications.Services;

public class FileTemplateService : ITemplateService
{
    private readonly string _basePath;
    private readonly FluidParser _parser;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, IFluidTemplate> _cache;

    public FileTemplateService(string basePath)
    {
        _basePath = basePath;
        _parser = new FluidParser();
        _cache = new System.Collections.Concurrent.ConcurrentDictionary<string, IFluidTemplate>();
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
                return string.Empty;
            }

            var source = await File.ReadAllTextAsync(path);
            if (!_parser.TryParse(source, out template, out var error))
            {
                throw new Exception($"Failed to parse template {path}: {error}");
            }
            
            _cache.TryAdd(cacheKey, template);
        }

        var options = new TemplateOptions();
        options.MemberAccessStrategy.Register(model.GetType());
        var context = new TemplateContext(model, options);
        return await template.RenderAsync(context);
    }
}
