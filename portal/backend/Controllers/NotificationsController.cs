using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrimusSaaS.Portal.Api.Data;
using PrimusSaaS.Portal.Api.Models;
using System.Security.Claims;

namespace PrimusSaaS.Portal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly PortalDbContext _context;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(PortalDbContext context, ILogger<NotificationsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("preferences")]
    public async Task<ActionResult<NotificationPreference>> GetPreferences()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        var pref = await _context.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (pref == null)
        {
            // Return default preferences if none exist
            return Ok(new NotificationPreference 
            { 
                UserId = userId,
                EmailOnNewVersion = true,
                EmailOnBreakingChange = true,
                EmailOnSecurityUpdate = true
            });
        }

        return Ok(pref);
    }

    [HttpPut("preferences")]
    public async Task<ActionResult<NotificationPreference>> UpdatePreferences([FromBody] NotificationPreference request)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        var pref = await _context.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (pref == null)
        {
            pref = new NotificationPreference
            {
                UserId = userId
            };
            _context.NotificationPreferences.Add(pref);
        }

        pref.EmailOnNewVersion = request.EmailOnNewVersion;
        pref.EmailOnBreakingChange = request.EmailOnBreakingChange;
        pref.EmailOnSecurityUpdate = request.EmailOnSecurityUpdate;
        pref.AdditionalEmails = request.AdditionalEmails;
        pref.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(pref);
    }
}
