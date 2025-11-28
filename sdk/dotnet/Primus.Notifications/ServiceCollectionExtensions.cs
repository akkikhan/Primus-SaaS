using System;
using Microsoft.Extensions.DependencyInjection;
using Primus.Notifications.Abstractions;
using Primus.Notifications.Channels.Email;
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
        _services.Configure(configureOptions);
        _services.AddScoped<IChannel, SmtpEmailChannel>();
        return this;
    }

    public PrimusNotificationBuilder UseFileTemplates(string basePath)
    {
        _services.AddSingleton<ITemplateService>(new FileTemplateService(basePath));
        return this;
    }

    public PrimusNotificationBuilder UseLogger()
    {
        _services.AddScoped<IChannel, Primus.Notifications.Channels.LoggerChannel>();
        return this;
    }
}
