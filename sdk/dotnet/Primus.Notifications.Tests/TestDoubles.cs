using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PrimusSaaS.Notifications.Abstractions;
using PrimusSaaS.Notifications.Core;

namespace PrimusSaaS.Notifications.Tests;

internal record TestNotification(
    string Type,
    object Data,
    Recipient Recipient,
    params string[] ChannelList) : INotification
{
    public IEnumerable<string> Channels => ChannelList;
}

internal sealed class RecordingChannel : IChannel
{
    private readonly TaskCompletionSource<bool> _sendTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _sendCount;

    public RecordingChannel(string name)
    {
        Name = name;
    }

    public string Name { get; }
    public int SendCount => _sendCount;
    public Task SendTask => _sendTcs.Task;

    public Task SendAsync(INotification notification, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _sendCount);
        _sendTcs.TrySetResult(true);
        return Task.CompletedTask;
    }
}

internal sealed class CapturingChannel : IChannel
{
    public CapturingChannel(string name)
    {
        Name = name;
    }

    public string Name { get; }
    public INotification? LastNotification { get; private set; }

    public Task SendAsync(INotification notification, CancellationToken cancellationToken = default)
    {
        LastNotification = notification;
        return Task.CompletedTask;
    }
}

internal sealed class RecordingSmsSender : ISmsSender
{
    public int SendCount { get; private set; }
    public string? LastTo { get; private set; }
    public string? LastMessage { get; private set; }

    public Task SendAsync(string to, string message, CancellationToken cancellationToken = default)
    {
        SendCount++;
        LastTo = to;
        LastMessage = message;
        return Task.CompletedTask;
    }
}

internal sealed class FailingChannel : IChannel
{
    private int _attempts;

    public FailingChannel(string name)
    {
        Name = name;
    }

    public string Name { get; }
    public int Attempts => _attempts;

    public Task SendAsync(INotification notification, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _attempts);
        throw new InvalidOperationException("Channel failure for test");
    }
}

internal sealed class ThrowingChannel : IChannel
{
    private readonly Exception _exception;

    public ThrowingChannel(string name, Exception exception)
    {
        Name = name;
        _exception = exception;
    }

    public string Name { get; }

    public Task SendAsync(INotification notification, CancellationToken cancellationToken = default)
    {
        throw _exception;
    }
}

internal sealed class SingleServiceScopeFactory : IServiceScopeFactory
{
    private readonly NotificationService _notificationService;

    public SingleServiceScopeFactory(NotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public IServiceScope CreateScope() => new SingleScope(_notificationService);

    private sealed class SingleScope : IServiceScope
    {
        private readonly NotificationService _notificationService;

        public SingleScope(NotificationService notificationService)
        {
            _notificationService = notificationService;
            ServiceProvider = new SingleServiceProvider(notificationService);
        }

        public IServiceProvider ServiceProvider { get; }

        public void Dispose()
        {
            // No scoped resources to dispose in tests.
        }
    }

    private sealed class SingleServiceProvider : IServiceProvider
    {
        private readonly NotificationService _notificationService;

        public SingleServiceProvider(NotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public object? GetService(Type serviceType)
        {
            return serviceType == typeof(NotificationService) ? _notificationService : null;
        }
    }
}

internal sealed class InMemoryTemplateServiceStub : ITemplateService
{
    private readonly string _value;

    public InMemoryTemplateServiceStub(string value)
    {
        _value = value;
    }

    public Task<string> RenderAsync(string notificationType, string channel, object model)
    {
        return Task.FromResult(_value);
    }
}
