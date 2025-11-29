using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrimusSaaS.Notifications.Abstractions;
using PrimusSaaS.Portal.Api.Notifications;
using System.Security.Claims;
using System.Diagnostics;

namespace PrimusSaaS.Portal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationQueue _notificationQueue;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(INotificationQueue notificationQueue, ILogger<NotificationsController> logger)
    {
        _notificationQueue = notificationQueue;
        _logger = logger;
    }

    [HttpPost("test")]
    public async Task<IActionResult> SendTestNotification([FromBody] TestNotificationRequest request)
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? "test@example.com";
        var name = User.Identity?.Name ?? "Test User";

        // Create a dummy notification
        // We reuse ApplicationCreatedNotification for the test
        var notification = new ApplicationCreatedNotification(
            "Test Application", 
            "client_test_12345", 
            email
        );

        await _notificationQueue.EnqueueAsync(notification);

        return Ok(new { message = "Notification queued successfully", recipient = email });
    }

    [HttpPost("load-test")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> LoadTest([FromBody] LoadTestRequest request)
    {
        var count = Math.Clamp(request.Count, 1, 1000);
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? "test@example.com";

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < count; i++)
        {
            var note = new ApplicationCreatedNotification(
                $"LoadTestApp-{i}",
                $"client_test_{i:D5}",
                email
            );
            await _notificationQueue.EnqueueAsync(note);
        }
        sw.Stop();

        _logger.LogInformation("Queued {Count} notifications for load test in {ElapsedMs}ms", count, sw.ElapsedMilliseconds);

        return Ok(new
        {
            message = "Queued notifications",
            queued = count,
            elapsedMs = sw.ElapsedMilliseconds
        });
    }
}

public class TestNotificationRequest
{
    public string Type { get; set; } = "ApplicationCreated";
}

public class LoadTestRequest
{
    public int Count { get; set; } = 100;
}
