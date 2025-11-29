using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrimusSaaS.Notifications.Abstractions;
using PrimusSaaS.Notifications.Configuration;

namespace PrimusSaaS.Notifications.Channels.Sms;

/// <summary>
/// SMS sender implementation using Twilio REST API.
/// </summary>
public class TwilioSmsSender : ISmsSender
{
    private readonly HttpClient _httpClient;
    private readonly TwilioOptions _options;
    private readonly ILogger<TwilioSmsSender> _logger;
    private readonly string _baseUrl;

    public TwilioSmsSender(
        HttpClient httpClient,
        IOptions<TwilioOptions> options,
        ILogger<TwilioSmsSender> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _options.Validate();

        _baseUrl = $"https://api.twilio.com/2010-04-01/Accounts/{_options.AccountSid}/Messages.json";

        // Set up Basic Auth
        var authBytes = Encoding.ASCII.GetBytes($"{_options.AccountSid}:{_options.AuthToken}");
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
    }

    public async Task SendAsync(string to, string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(to))
            throw new ArgumentException("Recipient phone number is required.", nameof(to));

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message body is required.", nameof(message));

        // Normalize phone number to E.164 format
        var normalizedTo = NormalizePhoneNumber(to);

        _logger.LogDebug("Sending SMS via Twilio to {To}", normalizedTo);

        var formData = new Dictionary<string, string>
        {
            ["To"] = normalizedTo,
            ["Body"] = message
        };

        // Use MessagingServiceSid if available, otherwise use FromNumber
        if (!string.IsNullOrWhiteSpace(_options.MessagingServiceSid))
        {
            formData["MessagingServiceSid"] = _options.MessagingServiceSid;
        }
        else
        {
            formData["From"] = _options.FromNumber;
        }

        var content = new FormUrlEncodedContent(formData);

        try
        {
            var response = await _httpClient.PostAsync(_baseUrl, content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<TwilioMessageResponse>(responseBody);
                _logger.LogInformation(
                    "SMS sent successfully via Twilio. SID: {MessageSid}, To: {To}, Status: {Status}",
                    result?.Sid, normalizedTo, result?.Status);
            }
            else
            {
                var error = JsonSerializer.Deserialize<TwilioErrorResponse>(responseBody);
                _logger.LogError(
                    "Twilio SMS failed. Status: {StatusCode}, Code: {ErrorCode}, Message: {ErrorMessage}",
                    (int)response.StatusCode, error?.Code, error?.Message);

                throw new TwilioSmsException(
                    $"Twilio SMS failed: {error?.Message ?? "Unknown error"}",
                    error?.Code ?? 0,
                    (int)response.StatusCode);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error sending SMS via Twilio to {To}", normalizedTo);
            throw new TwilioSmsException("Failed to connect to Twilio API.", ex);
        }
    }

    private static string NormalizePhoneNumber(string phone)
    {
        // Remove common formatting characters
        var cleaned = phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace(".", "");

        // If it doesn't start with +, assume US and add +1
        if (!cleaned.StartsWith("+", StringComparison.Ordinal))
        {
            if (cleaned.StartsWith("1", StringComparison.Ordinal) && cleaned.Length == 11)
            {
                cleaned = "+" + cleaned;
            }
            else if (cleaned.Length == 10)
            {
                cleaned = "+1" + cleaned;
            }
            else
            {
                cleaned = "+" + cleaned;
            }
        }

        return cleaned;
    }

    private class TwilioMessageResponse
    {
        public string? Sid { get; set; }
        public string? Status { get; set; }
        public string? To { get; set; }
        public string? From { get; set; }
        public string? Body { get; set; }
    }

    private class TwilioErrorResponse
    {
        public int Code { get; set; }
        public string? Message { get; set; }
        public string? MoreInfo { get; set; }
        public int Status { get; set; }
    }
}

/// <summary>
/// Exception thrown when Twilio SMS operations fail.
/// </summary>
public class TwilioSmsException : Exception
{
    /// <summary>
    /// Twilio error code (if available).
    /// </summary>
    public int TwilioErrorCode { get; }

    /// <summary>
    /// HTTP status code from the API response.
    /// </summary>
    public int HttpStatusCode { get; }

    public TwilioSmsException(string message) : base(message) { }

    public TwilioSmsException(string message, int twilioErrorCode, int httpStatusCode)
        : base(message)
    {
        TwilioErrorCode = twilioErrorCode;
        HttpStatusCode = httpStatusCode;
    }

    public TwilioSmsException(string message, Exception innerException)
        : base(message, innerException) { }
}
