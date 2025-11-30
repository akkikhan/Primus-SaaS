using LiveDemoApi.Models;
using Microsoft.AspNetCore.Mvc;
using PrimusSaaS.Notifications;
using PrimusSaaS.Notifications.Abstractions;
using PrimusSaaS.Notifications.Core;
using PrimusSaaS.Notifications.Services;

namespace LiveDemoApi.Controllers;

[ApiController]
[Route("notifications")]
public class NotificationsController : ControllerBase
{
    private readonly NotificationHealthService _health;
    private readonly INotificationService _notifications;
    private readonly ILogger<NotificationsController> _logger;
    private readonly IWebHostEnvironment _environment;

    public NotificationsController(
        NotificationHealthService health,
        INotificationService notifications,
        ILogger<NotificationsController> logger,
        IWebHostEnvironment environment)
    {
        _health = health;
        _notifications = notifications;
        _logger = logger;
        _environment = environment;
    }

    [HttpGet("health")]
    public async Task<IActionResult> Health()
    {
        var snapshot = await _health.GetChannelHealthAsync();
        _logger.LogInformation("Notifications health check snapshot: {@Snapshot}", snapshot);
        return new JsonResult(snapshot);
    }

    [HttpPost("welcome")]
    public async Task<IActionResult> Welcome(SendWelcomeRequest request)
    {
        LogJson("Notifications - welcome request", request);
        var notification = new BasicNotification(
            type: "Welcome",
            data: new { request.Name },
            recipient: new Recipient { Email = request.Email, Name = request.Name },
            channels: new[] { "Email", "Logger" });

        var result = await _notifications.SendAsync(notification);
        if (result.Success)
        {
            LogJson("Notifications - welcome sent", result);
            return Ok(new { message = "Notification dispatched", channel = result.ChannelUsed, queued = result.EnqueuedForRetry });
        }

        _logger.LogWarning("Notification failed: {Reason}", result.FailureReason);
        LogJson("Notifications - welcome failed", result);
        return Problem(detail: result.FailureReason ?? "Failed to dispatch notification.", statusCode: StatusCodes.Status400BadRequest);
    }

    [HttpPost("sms")]
    public async Task<IActionResult> Sms(SendSmsRequest request)
    {
        LogJson("Notifications - sms request", request);
        var notification = new BasicNotification(
            type: "SmsDemo",
            data: new { request.Message, request.PhoneNumber },
            recipient: new Recipient { PhoneNumber = request.PhoneNumber },
            channels: new[] { "Sms" });

        var result = await _notifications.SendAsync(notification);
        if (result.Success)
        {
            LogJson("Notifications - sms sent", result);
            return Ok(new { message = "SMS dispatched", channel = result.ChannelUsed, queued = result.EnqueuedForRetry });
        }

        _logger.LogWarning("SMS failed: {Reason}", result.FailureReason);
        LogJson("Notifications - sms failed", result);
        return Problem(detail: result.FailureReason ?? "Failed to dispatch SMS.", statusCode: StatusCodes.Status400BadRequest);
    }

    private void LogJson(string message, object data)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(data);
            _logger.LogInformation("{Message}: {Payload}", message, json);
        }
        catch
    {
        _logger.LogInformation("{Message}: (unserializable payload)", message);
        }
    }

    [HttpPost("test")]
    public async Task<IActionResult> Test(NotificationTestRequest request)
    {
        LogJson("Notifications - password reset test request", request);
        var notification = new BasicNotification(
            type: "PasswordReset",
            data: new { request.Name, request.Code, request.Link },
            recipient: new Recipient { Email = request.Email, Name = request.Name },
            channels: new[] { "Email", "Logger" });

        var result = await _notifications.SendAsync(notification);
        if (result.Success)
        {
            LogJson("Notifications - password reset sent", result);
            return Ok(new { message = "Password reset dispatched", channel = result.ChannelUsed, queued = result.EnqueuedForRetry });
        }

        _logger.LogWarning("Password reset failed: {Reason}", result.FailureReason);
        LogJson("Notifications - password reset failed", result);
        return Problem(detail: result.FailureReason ?? "Failed to dispatch notification.", statusCode: StatusCodes.Status400BadRequest);
    }

    [HttpGet("templates")]
    public IActionResult ListTemplates()
    {
        var root = GetTemplateRoot();
        if (!Directory.Exists(root))
        {
            return Ok(new { templates = Array.Empty<object>() });
        }

        var templates = Directory.GetDirectories(root)
            .Select(dir => new
            {
                type = Path.GetFileName(dir),
                channels = Directory.GetFiles(dir, "*.liquid")
                    .Select(f => Path.GetFileNameWithoutExtension(f))
                    .ToArray()
            })
            .ToArray();

        return Ok(new { templates });
    }

    [HttpGet("templates/{type}/{channel}")]
    public IActionResult GetTemplate(string type, string channel)
    {
        var path = GetTemplatePath(type, channel);
        if (!System.IO.File.Exists(path))
        {
            return NotFound(new { error = "Template not found." });
        }

        var content = System.IO.File.ReadAllText(path);
        return Ok(new { type, channel, content });
    }

    [HttpPut("templates/{type}/{channel}")]
    public IActionResult UpdateTemplate(string type, string channel, TemplateUpdateRequest request)
    {
        var path = GetTemplatePath(type, channel);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        System.IO.File.WriteAllText(path, request.Content ?? string.Empty);
        _logger.LogInformation("Template updated: {Type}/{Channel}", type, channel);
        return Ok(new { type, channel, saved = true });
    }

    [HttpPost("templates/preview")]
    public IActionResult PreviewTemplate(TemplatePreviewRequest request)
    {
        var template = request.Content;
        if (string.IsNullOrWhiteSpace(template))
        {
            var path = GetTemplatePath(request.Type, request.Channel);
            if (!System.IO.File.Exists(path))
            {
                return NotFound(new { error = "Template not found for preview." });
            }
            template = System.IO.File.ReadAllText(path);
        }

        var preview = ApplyPreviewVariables(template!, request);
        _logger.LogInformation("Previewed template {Type}/{Channel}", request.Type, request.Channel);
        return Ok(new { content = preview });
    }

    private string GetTemplateRoot() => Path.Combine(_environment.ContentRootPath, "NotificationTemplates");

    private string GetTemplatePath(string type, string channel)
    {
        var sanitizedChannel = channel.EndsWith(".liquid", StringComparison.OrdinalIgnoreCase)
            ? channel
            : $"{channel}.liquid";
        return Path.Combine(GetTemplateRoot(), type, sanitizedChannel);
    }

    private static string ApplyPreviewVariables(string template, TemplatePreviewRequest request)
    {
        var result = template;
        var replacements = new Dictionary<string, string?>
        {
            ["{{name}}"] = request.Name,
            ["{{email}}"] = request.Email,
            ["{{code}}"] = request.Code ?? "123456",
            ["{{link}}"] = request.Link ?? "http://localhost:5174",
            ["{{message}}"] = request.Message,
            ["{{phoneNumber}}"] = request.PhoneNumber
        };

        foreach (var kvp in replacements)
        {
            if (kvp.Value != null)
            {
                result = result.Replace(kvp.Key, kvp.Value);
            }
        }

        return result;
    }
}
