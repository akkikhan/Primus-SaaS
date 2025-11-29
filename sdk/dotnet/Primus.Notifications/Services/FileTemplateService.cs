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
    private readonly FileSystemWatcher? _watcher;

    public string BasePath => _basePath;

    public FileTemplateService(string basePath, ILogger<FileTemplateService>? logger = null, bool watchForChanges = false)
    {
        _basePath = basePath;
        _parser = new FluidParser();
        _cache = new System.Collections.Concurrent.ConcurrentDictionary<string, IFluidTemplate>();
        _logger = logger ?? NullLogger<FileTemplateService>.Instance;

        if (watchForChanges && Directory.Exists(basePath))
        {
            _watcher = new FileSystemWatcher(basePath, "*.liquid")
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };
            _watcher.Changed += (_, e) => _cache.TryRemove(e.FullPath, out IFluidTemplate? _);
            _watcher.Created += (_, e) => _cache.TryRemove(e.FullPath, out IFluidTemplate? _);
            _watcher.Deleted += (_, e) => _cache.TryRemove(e.FullPath, out IFluidTemplate? _);
            _watcher.Renamed += (_, e) =>
            {
                _cache.TryRemove(e.OldFullPath, out IFluidTemplate? _);
                _cache.TryRemove(e.FullPath, out IFluidTemplate? _);
            };
            _logger.LogInformation("Template watcher enabled for {BasePath}", basePath);
        }
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
            if (!_parser.TryParse(source, out IFluidTemplate? parsed, out var error))
            {
                _logger.LogError("Failed to parse template {TemplatePath}: {Error}", path, error);
                throw new Exception($"Failed to parse template {path}: {error}");
            }
            
            template = parsed;
            _cache.TryAdd(cacheKey, template);
        }

        var effectiveModel = model ?? new { };
        var options = new TemplateOptions();
        options.MemberAccessStrategy.Register(effectiveModel.GetType());
        var context = new TemplateContext(effectiveModel, options);
        return await template.RenderAsync(context);
    }

    /// <summary>
    /// Parses all .liquid templates under the base path to catch syntax errors early.
    /// </summary>
    public async Task ValidateAllAsync()
    {
        if (!Directory.Exists(_basePath))
        {
            _logger.LogWarning("Template base path does not exist: {Path}", _basePath);
            return;
        }

        var files = Directory.EnumerateFiles(_basePath, "*.liquid", SearchOption.AllDirectories).ToList();
        foreach (var file in files)
        {
            try
            {
                var source = await File.ReadAllTextAsync(file);
                if (!_parser.TryParse(source, out IFluidTemplate? template, out var error))
                {
                    _logger.LogError("Template parse failed for {Template}: {Error}", file, error);
                    continue;
                }

                _cache.TryAdd(file, template);
                _logger.LogDebug("Validated template {Template}", file);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating template {Template}", file);
            }
        }
    }
}
