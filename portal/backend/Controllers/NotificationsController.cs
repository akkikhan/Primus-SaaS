using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Primus.Notifications.Core;
using PrimusSaaS.Portal.Api.Notifications;
using System.Security.Claims;

namespace PrimusSaaS.Portal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly NotificationService _notificationService;

    public NotificationsController(NotificationService notificationService)
    {
        _notificationService = notificationService;
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

        await _notificationService.SendAsync(notification);

        return Ok(new { message = "Notification dispatched successfully", recipient = email });
    }
}

public class TestNotificationRequest
{
    public string Type { get; set; } = "ApplicationCreated";
}
