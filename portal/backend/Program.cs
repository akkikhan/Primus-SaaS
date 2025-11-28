using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PrimusSaaS.Portal.Api.Data;
using PrimusSaaS.Portal.Api.Services;
using System.Text;
using AspNetCoreRateLimit;
using Primus.Notifications;

var builder = WebApplication.CreateBuilder(args);

// Add database context - Use SQLite for easy testing
builder.Services.AddDbContext<PortalDbContext>(options =>
    options.UseSqlite("Data Source=portal.db"));

// Register services
builder.Services.AddScoped<IWebhookSignatureValidator, WebhookSignatureValidator>();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IModuleOwnershipService, ModuleOwnershipService>();

// Register Primus Notifications
builder.Services.AddPrimusNotifications(config =>
{
    var emailSettings = builder.Configuration.GetSection("EmailSettings").Get<EmailSettings>();
    if (emailSettings != null)
    {
        config.UseSmtp(options =>
        {
            options.Host = emailSettings.SmtpHost;
            options.Port = emailSettings.SmtpPort;
            options.Username = emailSettings.SmtpUser;
            options.Password = emailSettings.SmtpPass;
            options.EnableSsl = emailSettings.EnableSsl;
            options.FromAddress = emailSettings.FromAddress;
            options.FromName = "Primus SaaS";
        });
    }
    
    config.UseFileTemplates(Path.Combine(builder.Environment.ContentRootPath, "Templates"));
    config.UseLogger();
});

// Add rate limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// Add Azure AD authentication
var azureAdConfig = builder.Configuration.GetSection("AzureAd");
var tenantId = azureAdConfig["TenantId"];
var clientId = azureAdConfig["ClientId"];

Console.WriteLine($"[Azure AD Config] Tenant ID: {tenantId}");
Console.WriteLine($"[Azure AD Config] Client ID: {clientId}");
Console.WriteLine($"[Azure AD Config] Authority: https://login.microsoftonline.com/{tenantId}/v2.0");
Console.WriteLine($"[Azure AD Config] Audience: {clientId}");

// Configure JWT authentication to use local signing key for session tokens
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!))
        };
        
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"[Auth Failed] {context.Exception.Message}");
                Console.WriteLine($"[Auth Failed] Token: {context.Request.Headers["Authorization"]}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine($"[Token Validated] User: {context.Principal?.Identity?.Name}");
                Console.WriteLine($"[Token Validated] Claims: {string.Join(", ", context.Principal?.Claims.Select(c => $"{c.Type}={c.Value}") ?? new string[0])}");
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                Console.WriteLine($"[Auth Challenge] Error: {context.Error}, Description: {context.ErrorDescription}");
                return Task.CompletedTask;
            },
            OnMessageReceived = context =>
            {
                var token = context.Request.Headers["Authorization"].ToString();
                Console.WriteLine($"[Message Received] Authorization Header: {(string.IsNullOrEmpty(token) ? "NONE" : "Present")}");
                if (!string.IsNullOrEmpty(token))
                {
                    Console.WriteLine($"[Message Received] Token Prefix: {token.Substring(0, Math.Min(20, token.Length))}...");
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection(); // Commented out for HTTP testing

app.UseIpRateLimiting();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Initialize database and apply migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PortalDbContext>();
    db.Database.Migrate();
}

app.Run();
