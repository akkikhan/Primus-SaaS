using Microsoft.AspNetCore.Authorization;
using PrimusSaaS.Identity.Validator;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Primus Identity Validation
builder.Services.AddPrimusIdentity(options =>
{
    options.PortalUrl = builder.Configuration["PrimusIdentity:PortalUrl"] 
        ?? "https://portal.primus-saas.com";
    options.ClientId = builder.Configuration["PrimusIdentity:ClientId"] 
        ?? throw new InvalidOperationException("PrimusIdentity:ClientId is required");
    options.ClientSecret = builder.Configuration["PrimusIdentity:ClientSecret"] 
        ?? throw new InvalidOperationException("PrimusIdentity:ClientSecret is required");
    options.JwtSecret = builder.Configuration["PrimusIdentity:JwtSecret"] 
        ?? throw new InvalidOperationException("PrimusIdentity:JwtSecret is required");
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enable authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Example: Public endpoint
app.MapGet("/api/public", () => new { 
    message = "This is a public endpoint accessible without authentication" 
})
.WithName("GetPublicData")
.WithOpenApi();

// Example: Protected endpoint requiring authentication
app.MapGet("/api/protected", [Authorize] (HttpContext context) =>
{
    var user = context.GetPrimusUser();
    return new
    {
        message = "This endpoint requires authentication",
        user = new
        {
            userId = user?.UserId,
            email = user?.Email,
            name = user?.Name,
            roles = user?.Roles
        }
    };
})
.WithName("GetProtectedData")
.WithOpenApi();

// Example: Role-based protected endpoint
app.MapGet("/api/admin", [Authorize(Roles = "Admin")] (HttpContext context) =>
{
    var user = context.GetPrimusUser();
    return new
    {
        message = "This endpoint requires Admin role",
        user = new
        {
            userId = user?.UserId,
            email = user?.Email,
            name = user?.Name,
            roles = user?.Roles
        }
    };
})
.WithName("GetAdminData")
.WithOpenApi();

app.Run();
