using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using PrimusSaaS.Notifications.Abstractions;
using PrimusSaaS.Notifications.Channels.Email;
using PrimusSaaS.Notifications.Channels.Sms;
using PrimusSaaS.Notifications.Configuration;
using PrimusSaaS.Notifications.Core;
using PrimusSaaS.Notifications.Services;

namespace PrimusSaaS.Notifications;

/// <summary>
/// Extension methods for configuring PrimusSaaS Notifications services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds PrimusSaaS Notifications services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configure">A delegate to configure the notification builder.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddPrimusNotifications(notifications =>
    /// {
    ///     notifications
    ///         .UseSmtp(opts => { opts.Host = "smtp.example.com"; /* ... */ })
    ///         .UseLogger();
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddPrimusNotifications(this IServiceCollection services, Action<PrimusNotificationBuilder> configure)
    {
        var builder = new PrimusNotificationBuilder(services);
        configure(builder);

        services.AddOptions<NotificationOptions>();
        
        // Register NotificationService as both the concrete type and the interface
        // This allows consumers to inject either INotificationService (recommended) or NotificationService directly
        services.AddScoped<NotificationService>();
        services.AddScoped<INotificationService>(sp => sp.GetRequiredService<NotificationService>());
        
        return services;
    }
}

/// <summary>
/// Fluent builder for configuring PrimusSaaS Notifications.
/// Use this with <see cref="ServiceCollectionExtensions.AddPrimusNotifications"/> to configure email, SMS, templates, and queuing.
/// </summary>
public class PrimusNotificationBuilder
{
    private readonly IServiceCollection _services;

    public PrimusNotificationBuilder(IServiceCollection services)
    {
        _services = services;
    }

    /// <summary>
    /// Configures general notification dispatch options.
    /// </summary>
    /// <param name="configureOptions">Action to configure notification options.</param>
    /// <returns>The builder for chaining.</returns>
    public PrimusNotificationBuilder ConfigureDispatch(Action<NotificationOptions> configureOptions)
    {
        _services.Configure(configureOptions);
        return this;
    }

    /// <summary>
    /// Configures SMTP as the email delivery channel.
    /// </summary>
    /// <param name="configureOptions">Action to configure SMTP settings (host, port, credentials, etc.).</param>
    /// <returns>The builder for chaining.</returns>
    /// <example>
    /// <code>
    /// notifications.UseSmtp(opts =>
    /// {
    ///     opts.Host = "smtp.example.com";
    ///     opts.Port = 587;
    ///     opts.Username = "user";
    ///     opts.Password = "password";
    ///     opts.FromAddress = "no-reply@example.com";
    ///     opts.EnableSsl = true;
    /// });
    /// </code>
    /// </example>
    public PrimusNotificationBuilder UseSmtp(Action<SmtpOptions> configureOptions)
    {
        _services.Configure<SmtpOptions>(opts =>
        {
            configureOptions(opts);
            opts.Validate();
        });
        _services.AddScoped<IChannel, SmtpEmailChannel>();
        return this;
    }

    /// <summary>
    /// Enables SMS notifications using the default logging sender (for development/testing).
    /// To send real SMS messages, use <see cref="UseTwilio(Action{TwilioOptions})"/> or provide a custom <see cref="ISmsSender"/>.
    /// </summary>
    /// <param name="configureOptions">Optional action to configure SMS options.</param>
    /// <returns>The builder for chaining.</returns>
    public PrimusNotificationBuilder UseSms(Action<SmsOptions>? configureOptions = null)
    {
        _services.Configure<SmsOptions>(opts =>
        {
            configureOptions?.Invoke(opts);
            opts.Validate();
        });

        _services.TryAddScoped<ISmsSender, LoggingSmsSender>();
        _services.AddScoped<IChannel, SmsChannel>();
        return this;
    }

    /// <summary>
    /// Enables SMS notifications using a custom SMS sender implementation.
    /// </summary>
    /// <typeparam name="TSender">The custom SMS sender type implementing <see cref="ISmsSender"/>.</typeparam>
    /// <param name="configureOptions">Optional action to configure SMS options.</param>
    /// <returns>The builder for chaining.</returns>
    public PrimusNotificationBuilder UseSms<TSender>(Action<SmsOptions>? configureOptions = null)
        where TSender : class, ISmsSender
    {
        _services.Configure<SmsOptions>(opts =>
        {
            configureOptions?.Invoke(opts);
            opts.Validate();
        });

        _services.AddScoped<ISmsSender, TSender>();
        _services.AddScoped<IChannel, SmsChannel>();
        return this;
    }

    /// <summary>
    /// Enables file-based Liquid templates for notifications.
    /// Templates should be organized as: {basePath}/{NotificationType}/EmailSubject.liquid, EmailBody.liquid, SmsBody.liquid.
    /// </summary>
    /// <param name="basePath">The root directory containing notification templates.</param>
    /// <returns>The builder for chaining.</returns>
    public PrimusNotificationBuilder UseFileTemplates(string basePath)
    {
        _services.AddSingleton<ITemplateService>(sp =>
            new FileTemplateService(basePath, sp.GetService<ILogger<FileTemplateService>>()));
        return this;
    }

    /// <summary>
    /// Enables a logger channel that writes notification details to the application logs.
    /// Useful for development, testing, or as a fallback channel.
    /// </summary>
    /// <returns>The builder for chaining.</returns>
    public PrimusNotificationBuilder UseLogger()
    {
        _services.AddScoped<IChannel, PrimusSaaS.Notifications.Channels.LoggerChannel>();
        return this;
    }

    /// <summary>
    /// Enables an in-memory bounded queue with a background worker for async notification delivery.
    /// Recommended for decoupling notification dispatch from request processing.
    /// </summary>
    /// <param name="configureOptions">Optional action to configure queue capacity, parallelism, and retry behavior.</param>
    /// <returns>The builder for chaining.</returns>
    public PrimusNotificationBuilder UseInMemoryQueue(Action<NotificationQueueOptions>? configureOptions = null)
    {
        if (configureOptions != null)
        {
            _services.Configure(configureOptions);
        }
        else
        {
            _services.Configure<NotificationQueueOptions>(_ => { });
        }

        _services.AddSingleton<InMemoryNotificationQueue>();
        _services.AddSingleton<INotificationQueue>(sp => sp.GetRequiredService<InMemoryNotificationQueue>());
        _services.AddHostedService<NotificationBackgroundService>();
        return this;
    }

    /// <summary>
    /// Configures Twilio as the SMS provider.
    /// </summary>
    /// <param name="configureOptions">Action to configure Twilio options.</param>
    /// <returns>The builder for chaining.</returns>
    public PrimusNotificationBuilder UseTwilio(Action<TwilioOptions> configureOptions)
    {
        _services.AddOptions<TwilioOptions>();
        _services.Configure<TwilioOptions>(opts =>
        {
            configureOptions(opts);
            if (opts.ValidateOnStartup)
            {
                opts.Validate();
            }
        });

        _services.AddHttpClient<TwilioSmsSender>();
        _services.AddScoped<ISmsSender, TwilioSmsSender>();
        _services.AddScoped<IChannel, SmsChannel>();
        return this;
    }

    /// <summary>
    /// Configures Twilio as the SMS provider using configuration from appsettings.json.
    /// Reads from the "Twilio" section by default.
    /// </summary>
    /// <param name="configuration">The configuration root.</param>
    /// <param name="sectionName">The configuration section name (default: "Twilio").</param>
    /// <param name="validateOnStartup">Whether to validate Twilio configuration on application startup (default: true).</param>
    /// <returns>The builder for chaining.</returns>
    public PrimusNotificationBuilder UseTwilio(Microsoft.Extensions.Configuration.IConfiguration configuration, string sectionName = TwilioOptions.SectionName, bool validateOnStartup = true)
    {
        _services.AddOptions<TwilioOptions>()
            .Bind(configuration.GetSection(sectionName))
            .PostConfigure(opts =>
            {
                if (validateOnStartup && opts.ValidateOnStartup)
                {
                    opts.Validate();
                }
            });

        if (validateOnStartup)
        {
            _services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, TwilioStartupValidator>());
        }

        _services.AddHttpClient<TwilioSmsSender>();
        _services.AddScoped<ISmsSender, TwilioSmsSender>();
        _services.AddScoped<IChannel, SmsChannel>();
        return this;
    }
}

internal sealed class TwilioStartupValidator : IHostedService
{
    private readonly IOptions<TwilioOptions> _options;

    public TwilioStartupValidator(IOptions<TwilioOptions> options)
    {
        _options = options;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var value = _options.Value;
        if (value.ValidateOnStartup)
        {
            value.Validate();
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
