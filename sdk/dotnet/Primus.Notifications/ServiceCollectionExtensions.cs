using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using Primus.Notifications.Abstractions;
using Primus.Notifications.Channels.Email;
using Primus.Notifications.Channels.Sms;
using Primus.Notifications.Configuration;
using Primus.Notifications.Core;
using Primus.Notifications.Services;

namespace Primus.Notifications;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPrimusNotifications(this IServiceCollection services, Action<PrimusNotificationBuilder> configure)
    {
        var builder = new PrimusNotificationBuilder(services);
        configure(builder);

        services.AddScoped<NotificationService>();
        
        // Register the service as the implementation of the core logic, 
        // but users might want to inject it directly or via an interface if we added one for the service itself.
        // For now, we register the concrete class.
        
        return services;
    }
}

public class PrimusNotificationBuilder
{
    private readonly IServiceCollection _services;

    public PrimusNotificationBuilder(IServiceCollection services)
    {
        _services = services;
    }

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

    public PrimusNotificationBuilder UseFileTemplates(string basePath)
    {
        _services.AddSingleton<ITemplateService>(sp =>
            new FileTemplateService(basePath, sp.GetService<ILogger<FileTemplateService>>()));
        return this;
    }

    public PrimusNotificationBuilder UseLogger()
    {
        _services.AddScoped<IChannel, Primus.Notifications.Channels.LoggerChannel>();
        return this;
    }

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
        _services.Configure<TwilioOptions>(opts =>
        {
            configureOptions(opts);
            opts.Validate();
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
    /// <returns>The builder for chaining.</returns>
    public PrimusNotificationBuilder UseTwilio(Microsoft.Extensions.Configuration.IConfiguration configuration, string sectionName = TwilioOptions.SectionName)
    {
        _services.Configure<TwilioOptions>(configuration.GetSection(sectionName));
        _services.AddHttpClient<TwilioSmsSender>();
        _services.AddScoped<ISmsSender, TwilioSmsSender>();
        _services.AddScoped<IChannel, SmsChannel>();
        return this;
    }
}
