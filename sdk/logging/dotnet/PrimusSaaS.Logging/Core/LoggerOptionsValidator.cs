using System.Text;

namespace PrimusSaaS.Logging.Core;

/// <summary>
/// Validates logger configuration and surfaces actionable errors.
/// </summary>
public static class LoggerOptionsValidator
{
    public static void Validate(LoggerOptions options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ApplicationId))
        {
            errors.Add("ApplicationId is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Environment))
        {
            errors.Add("Environment is required.");
        }

        var hasCustomTargets = options.CustomTargets is { Count: > 0 };
        var hasConfiguredTargets = options.Targets != null && options.Targets.Count > 0;

        if (!hasCustomTargets && !hasConfiguredTargets)
        {
            errors.Add("At least one target must be configured.");
        }
        else if (options.Targets is { Count: > 0 })
        {
            foreach (var target in options.Targets)
            {
                ValidateTarget(target, errors);
            }
        }

        options.Serialization ??= new SerializationOptions();
        options.Pii ??= new PiiOptions();
        try
        {
            options.Serialization.Validate();
        }
        catch (Exception ex)
        {
            errors.Add($"Serialization options invalid: {ex.Message}");
        }

        if (errors.Count > 0)
        {
            var builder = new StringBuilder("Invalid Primus logger configuration:");
            foreach (var error in errors)
            {
                builder.Append(' ').Append(error);
            }

            throw new ArgumentException(builder.ToString(), nameof(options));
        }
    }

    private static void ValidateTarget(TargetConfig config, List<string> errors)
    {
        if (config == null)
        {
            errors.Add("Target configuration cannot be null.");
            return;
        }

        if (string.IsNullOrWhiteSpace(config.Type))
        {
            errors.Add("Target type is required.");
            return;
        }

        switch (config.Type.ToLowerInvariant())
        {
            case "console":
            case "applicationinsights":
                break;
            case "file":
                if (config.MaxFileSize <= 0)
                {
                    errors.Add("File target MaxFileSize must be greater than zero.");
                }

                if (config.MaxRetainedFiles < 1)
                {
                    errors.Add("File target MaxRetainedFiles must be at least 1.");
                }
                break;
            default:
                errors.Add($"Unsupported target type '{config.Type}'.");
                break;
        }

        if (config.Async && config.BufferSize <= 0)
        {
            errors.Add("Async buffer size must be greater than zero.");
        }
    }
}
