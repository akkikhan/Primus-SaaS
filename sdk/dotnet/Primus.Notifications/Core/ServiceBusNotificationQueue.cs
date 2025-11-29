using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrimusSaaS.Notifications.Abstractions;
using PrimusSaaS.Notifications.Configuration;

namespace PrimusSaaS.Notifications.Core;

public sealed class ServiceBusNotificationQueue : INotificationQueue, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;
    private readonly ServiceBusReceiver _receiver;
    private readonly ServiceBusQueueOptions _options;
    private readonly ILogger<ServiceBusNotificationQueue> _logger;
    private readonly JsonSerializerOptions _serializerOptions;

    public ServiceBusNotificationQueue(IOptions<ServiceBusQueueOptions> options, ILogger<ServiceBusNotificationQueue> logger)
    {
        _options = options.Value ?? new ServiceBusQueueOptions();
        _logger = logger;
        _client = new ServiceBusClient(_options.ConnectionString);
        _sender = _client.CreateSender(_options.QueueName);
        _receiver = _client.CreateReceiver(_options.QueueName, new ServiceBusReceiverOptions
        {
            PrefetchCount = _options.PrefetchCount
        });
        _serializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    public async Task EnqueueAsync(INotification notification, CancellationToken cancellationToken = default)
    {
        var envelope = QueuedNotificationEnvelope.From(notification, _serializerOptions);
        var body = JsonSerializer.Serialize(envelope, _serializerOptions);
        var message = new ServiceBusMessage(body);
        await _sender.SendMessageAsync(message, cancellationToken);
    }

    public async ValueTask<INotification?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var message = await _receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(_options.ReceiveWaitTimeSeconds), cancellationToken);
            if (message == null)
            {
                return null;
            }

            var envelope = JsonSerializer.Deserialize<QueuedNotificationEnvelope>(message.Body.ToString(), _serializerOptions);
            await _receiver.CompleteMessageAsync(message, cancellationToken);
            return envelope?.ToNotification();
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Service Bus dequeue failed");
            return null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
        await _receiver.DisposeAsync();
        await _client.DisposeAsync();
    }
}
