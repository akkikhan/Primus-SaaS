using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrimusSaaS.Notifications.Abstractions;
using PrimusSaaS.Notifications.Configuration;

namespace PrimusSaaS.Notifications.Core;

public sealed class HttpNotificationWebhookDispatcher : INotificationWebhookDispatcher
{
    private readonly HttpClient _httpClient;
    private readonly WebhookOptions _options;
    private readonly ILogger<HttpNotificationWebhookDispatcher> _logger;

    public HttpNotificationWebhookDispatcher(
        HttpClient httpClient,
        IOptions<WebhookOptions> options,
        ILogger<HttpNotificationWebhookDispatcher> logger)
    {
        _httpClient = httpClient;
        _options = options.Value ?? new WebhookOptions();
        _logger = logger;
    }

    public async Task DispatchAsync(NotificationDeliveryRecord record, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.Endpoint))
        {
            return;
        }

        if (!_options.SendOnSuccess && record.Success)
        {
            return;
        }

        if (!_options.SendOnFailure && !record.Success)
        {
            return;
        }

        try
        {
            var payload = record;
            var response = await _httpClient.SendAsync(BuildRequest(payload), cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Webhook delivery returned status {Status}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch notification webhook");
        }
    }

    private HttpRequestMessage BuildRequest(NotificationDeliveryRecord payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint)
        {
            Content = JsonContent.Create(payload)
        };

        if (!string.IsNullOrWhiteSpace(_options.Secret))
        {
            var bodyBytes = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(payload));
            var signature = ComputeHmac(bodyBytes, _options.Secret);
            request.Headers.Add("X-Primus-Signature", signature);
        }

        return request;
    }

    private static string ComputeHmac(byte[] body, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(body);
        return Convert.ToHexString(hash);
    }
}
