using Microsoft.AspNetCore.Mvc;
using PrimusSaaS.FeatureFlags;

namespace LiveDemoApi.Controllers;

[ApiController]
[Route("feature-flags")]
public class FeatureFlagsController : ControllerBase
{
    private readonly IFeatureFlagService _featureFlags;
    private readonly ILogger<FeatureFlagsController> _logger;

    public FeatureFlagsController(IFeatureFlagService featureFlags, ILogger<FeatureFlagsController> logger)
    {
        _featureFlags = featureFlags;
        _logger = logger;
    }

    [HttpGet("test")]
    public IActionResult Test()
    {
        var user = HttpContext.User;
        var userId = user.FindFirst("sub")?.Value
                  ?? user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
                  ?? "anonymous";
        var email = user.FindFirst("email")?.Value
                 ?? user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;

        var evalContext = new FeatureFlagContext { UserId = userId, Email = email };
        var flags = _featureFlags.GetAllFlagsAsync().GetAwaiter().GetResult();

        var evaluations = flags.ToDictionary(
            kvp => kvp.Key,
            kvp =>
            {
                var definition = _featureFlags.GetFlagDefinitionAsync(kvp.Key).GetAwaiter().GetResult();
                return new
                {
                    globalEnabled = kvp.Value,
                    enabledForUser = _featureFlags.IsEnabled(kvp.Key, evalContext),
                    description = definition?.Description,
                    rolloutPercentage = definition?.RolloutPercentage
                };
            });

        _logger.LogInformation("✅ /feature-flags/test: evaluated {Count} flags", evaluations.Count);
        return Ok(new
        {
            user = new { userId, email, authenticated = user.Identity?.IsAuthenticated ?? false },
            flags = evaluations,
            timestamp = DateTime.UtcNow.ToString("o")
        });
    }
}
