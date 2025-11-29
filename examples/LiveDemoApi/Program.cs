using PrimusSaaS.Identity.Validator;
using PrimusSaaS.Logging.Extensions;
using PrimusSaaS.Notifications;
using PrimusSaaS.Notifications.Services;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// =========================================================================
// DEMO STEP 1: Register Primus Identity Services
// =========================================================================
// This single line binds the configuration from appsettings.json
// and sets up all necessary validation logic for multiple providers.
builder.Services.AddPrimusIdentity(options =>
{
    builder.Configuration.GetSection("PrimusIdentity").Bind(options);
});

// Register Primus Logging
builder.Logging.AddPrimusLogging(options =>
{
    builder.Configuration.GetSection("PrimusLogging").Bind(options);
});

// Optional: register notifications so the health endpoint can report status.
builder.Services.AddPrimusNotifications(notifications =>
{
    // Minimal logger-only setup; templates optional (no startup validation to avoid boot errors if missing).
    notifications.UseLogger();
    notifications.UseFileTemplates(Path.Combine(builder.Environment.ContentRootPath, "NotificationTemplates"), validateOnStartup: false);
});

// Standard ASP.NET Core Authorization
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// =========================================================================
// DEMO STEP 2: Add Middleware
// =========================================================================
// Ensure these are placed between UseHttpsRedirection and MapControllers
app.UseAuthentication();
app.UseAuthorization();

// =========================================================================
// DEMO STEP 3: Diagnostics (Optional)
// =========================================================================
// This endpoint (/primus/diagnostics) allows us to verify our configuration
// and see exactly which keys are loaded from Azure AD and Auth0.
app.MapPrimusIdentityDiagnostics();

app.MapGet("/health/notifications", async (NotificationHealthService health) =>
{
    var snapshot = await health.GetChannelHealthAsync();
    return Results.Json(snapshot);
});

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi()
.RequireAuthorization(); // <--- DEMO STEP 4: Secure the endpoint

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
