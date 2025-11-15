using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrimusSaaS.Identity.Validator;

namespace PrimusSaaS.Example.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WeatherController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherController> _logger;

    public WeatherController(ILogger<WeatherController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get weather forecast - requires authentication
    /// </summary>
    [HttpGet]
    [Authorize]
    public ActionResult<IEnumerable<WeatherForecast>> GetWeather()
    {
        var user = HttpContext.GetPrimusUser();
        
        _logger.LogInformation(
            "User {UserId} ({Email}) requested weather forecast", 
            user?.UserId, 
            user?.Email);

        return Ok(Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray());
    }

    /// <summary>
    /// Get extended weather forecast - requires Admin or Manager role
    /// </summary>
    [HttpGet("extended")]
    [Authorize(Roles = "Admin,Manager")]
    public ActionResult<IEnumerable<WeatherForecast>> GetExtendedWeather()
    {
        var user = HttpContext.GetPrimusUser();
        
        _logger.LogInformation(
            "User {UserId} with roles [{Roles}] requested extended forecast", 
            user?.UserId, 
            string.Join(", ", user?.Roles ?? new List<string>()));

        return Ok(Enumerable.Range(1, 14).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray());
    }
}

public record WeatherForecast
{
    public DateOnly Date { get; set; }
    public int TemperatureC { get; set; }
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    public string? Summary { get; set; }
}
