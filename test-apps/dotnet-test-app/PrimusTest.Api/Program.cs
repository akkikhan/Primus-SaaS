using Microsoft.AspNetCore.Authorization;
using PrimusSaaS.Identity.Validator;

var builder = WebApplication.CreateBuilder(args);

// Add Primus Identity Validator
builder.Services.AddPrimusIdentity(options =>
{
    options.Issuers = new List<IssuerConfig>
    {
        new IssuerConfig
        {
            Name = "LocalAuth",
            Type = IssuerType.Jwt,
            Issuer = "http://localhost:5267",
            Secret = builder.Configuration["PrimusIdentity:JwtSecret"] ?? "test-jwt-secret-key-with-at-least-32-characters",
            Audiences = new List<string> { "test-client-123" }
        },
        new IssuerConfig
        {
            Name = "AzureAD",
            Type = IssuerType.Oidc,
            Issuer = $"https://login.microsoftonline.com/{builder.Configuration["PrimusIdentity:TenantId"] ?? "common"}/v2.0",
            Authority = $"https://login.microsoftonline.com/{builder.Configuration["PrimusIdentity:TenantId"] ?? "common"}/v2.0",
            Audiences = new List<string> { "test-client-123" }
        }
    };
    
    // Optional: JWKS cache TTL (default is 24 hours)
    if (int.TryParse(builder.Configuration["PrimusIdentity:JwksCacheTtl"], out var cacheTtl))
    {
        options.JwksCacheTtl = TimeSpan.FromHours(cacheTtl);
    }
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Test endpoints
app.MapGet("/api/health", () => new { 
    status = "healthy", 
    message = "Primus SDK Test App is running",
    timestamp = DateTime.UtcNow 
}).WithName("Health");

app.MapGet("/api/public", () => new { 
    message = "This is a public endpoint - no authentication required" 
}).WithName("Public");

app.MapGet("/api/protected", [Authorize] (HttpContext context) =>
{
    var user = context.GetPrimusUser();
    return new
    {
        message = "Successfully authenticated!",
        user = new
        {
            userId = user?.UserId,
            email = user?.Email,
            name = user?.Name,
            roles = user?.Roles ?? new List<string>()
        },
        timestamp = DateTime.UtcNow
    };
}).WithName("Protected");

app.MapGet("/api/admin", [Authorize(Roles = "Admin")] (HttpContext context) =>
{
    var user = context.GetPrimusUser();
    return new
    {
        message = "Admin access granted!",
        user = new
        {
            userId = user?.UserId,
            email = user?.Email,
            roles = user?.Roles ?? new List<string>()
        }
    };
}).WithName("AdminOnly");

app.MapGet("/api/manager", [Authorize(Roles = "Manager,Admin")] (HttpContext context) =>
{
    var user = context.GetPrimusUser();
    return new
    {
        message = "Manager or Admin access granted!",
        user = new
        {
            userId = user?.UserId,
            email = user?.Email,
            roles = user?.Roles ?? new List<string>()
        }
    };
}).WithName("ManagerOrAdmin");

app.MapControllers();

Console.WriteLine("🚀 Primus SDK Test Application Starting...");
Console.WriteLine($"📦 Using Primus Portal: {builder.Configuration["PrimusIdentity:PortalUrl"] ?? "https://localhost:7001"}");
Console.WriteLine($"🔑 Client ID: {builder.Configuration["PrimusIdentity:ClientId"] ?? "test-client-123"}");
Console.WriteLine("✅ Primus Identity Validator configured successfully");

app.Run();
