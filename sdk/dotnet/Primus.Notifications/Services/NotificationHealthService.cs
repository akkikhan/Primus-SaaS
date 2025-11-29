using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrimusSaaS.Notifications.Abstractions;
using PrimusSaaS.Notifications.Configuration;
using PrimusSaaS.Notifications.Channels.Sms;

namespace PrimusSaaS.Notifications.Services;

public class NotificationHealthService
{
    private readonly IEnumerable<IChannel> _channels;
    private readonly ILogger<NotificationHealthService> _logger;
    private readonly IOptions<SmtpOptions>? _smtpOptions;
    private readonly IOptions<TwilioOptions>? _twilioOptions;

    public NotificationHealthService(
        IEnumerable<IChannel> channels,
        ILogger<NotificationHealthService> logger,
        IServiceProvider serviceProvider)
    {
        _channels = channels;
        _logger = logger;
        _smtpOptions = serviceProvider.GetService(typeof(IOptions<SmtpOptions>)) as IOptions<SmtpOptions>;
        _twilioOptions = serviceProvider.GetService(typeof(IOptions<TwilioOptions>)) as IOptions<TwilioOptions>;
    }

    public Task<NotificationHealthSnapshot> GetChannelHealthAsync()
    {
        var statuses = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (_channels.Any(c => c.Name.Equals("Email", StringComparison.OrdinalIgnoreCase)))
        {
            statuses["email"] = ValidateSmtp();
        }
        else
        {
            statuses["email"] = "not_configured";
        }

        if (_channels.Any(c => c.Name.Equals("Sms", StringComparison.OrdinalIgnoreCase)))
        {
            statuses["sms"] = ValidateSms();
        }
        else
        {
            statuses["sms"] = "not_configured";
        }

        if (_channels.Any(c => c.Name.Equals("Logger", StringComparison.OrdinalIgnoreCase)))
        {
            statuses["logger"] = "available";
        }

        return Task.FromResult(new NotificationHealthSnapshot(statuses));
    }

    private string ValidateSmtp()
    {
        try
        {
            var opts = _smtpOptions?.Value;
            if (opts == null)
            {
                return "misconfigured: smtp options missing";
            }

            opts.Validate();
            return "configured";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SMTP configuration validation failed.");
            return $"misconfigured: {ex.Message}";
        }
    }

    private string ValidateSms()
    {
        try
        {
            var opts = _twilioOptions?.Value;
            if (opts == null)
            {
                return "degraded: logging sender only or sms sender not configured";
            }

            opts.Validate();
            return "configured";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SMS configuration validation failed.");
            return $"misconfigured: {ex.Message}";
        }
    }
}

public sealed record NotificationHealthSnapshot(IReadOnlyDictionary<string, string> Channels);
