using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrimusSaaS.Portal.Api.Data;
using PrimusSaaS.Portal.Api.Models;
using PrimusSaaS.Notifications.Diagnostics;

namespace PrimusSaaS.Portal.Api.Controllers;

[ApiController]
[Route("api/notification-preferences")]
[Authorize]
public class NotificationPreferencesController : ControllerBase
{
    private readonly PortalDbContext _context;
    private readonly ILogger<NotificationPreferencesController> _logger;

    public NotificationPreferencesController(PortalDbContext context, ILogger<NotificationPreferencesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/notification-preferences/me
    [HttpGet("me")]
    public async Task<ActionResult<NotificationPreferenceDto>> GetMine()
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "User id missing from token." });
        }

        var pref = await _context.NotificationPreferences.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId.Value);

        if (pref == null)
        {
            pref = new NotificationPreference
            {
                UserId = userId.Value
            };
            _context.NotificationPreferences.Add(pref);
            await _context.SaveChangesAsync();
        }

        return Ok(ToDto(pref));
    }

    // PUT: api/notification-preferences/me
    [HttpPut("me")]
    public async Task<ActionResult<NotificationPreferenceDto>> UpsertMine([FromBody] UpdateNotificationPreferenceRequest request)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "User id missing from token." });
        }

        var pref = await _context.NotificationPreferences.FirstOrDefaultAsync(p => p.UserId == userId.Value);
        if (pref == null)
        {
            pref = new NotificationPreference { UserId = userId.Value };
            _context.NotificationPreferences.Add(pref);
        }

        pref.EmailOnNewVersion = request.EmailOnNewVersion;
        pref.EmailOnBreakingChange = request.EmailOnBreakingChange;
        pref.EmailOnSecurityUpdate = request.EmailOnSecurityUpdate;
        pref.AdditionalEmails = request.AdditionalEmails ?? string.Empty;
        pref.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated notification preferences for user {UserId}", userId.Value);

        return Ok(ToDto(pref));
    }

    private int? GetUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(idClaim, out var userId))
        {
            return userId;
        }

        return null;
    }

    private static NotificationPreferenceDto ToDto(NotificationPreference pref) =>
        new NotificationPreferenceDto(
            pref.EmailOnNewVersion,
            pref.EmailOnBreakingChange,
            pref.EmailOnSecurityUpdate,
            pref.AdditionalEmails
        );

    // GET: api/notification-preferences/metrics (simple diagnostics view)
    [HttpGet("metrics")]
    [Authorize(Roles = "Admin")]
    public ActionResult<object> GetMetrics()
    {
        var snapshot = NotificationRuntimeStats.GetSnapshot();
        return Ok(new
        {
            snapshot.Sent,
            snapshot.Failed,
            snapshot.Queued,
            snapshot.AvgDispatchDurationMs
        });
    }
}

public record NotificationPreferenceDto(
    bool EmailOnNewVersion,
    bool EmailOnBreakingChange,
    bool EmailOnSecurityUpdate,
    string? AdditionalEmails
);

public record UpdateNotificationPreferenceRequest(
    bool EmailOnNewVersion,
    bool EmailOnBreakingChange,
    bool EmailOnSecurityUpdate,
    string? AdditionalEmails
);
