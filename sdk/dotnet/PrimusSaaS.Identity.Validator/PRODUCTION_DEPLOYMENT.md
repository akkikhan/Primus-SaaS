# Production Deployment Guide - .NET SDK

Best practices for deploying **PrimusSaaS.Identity.Validator** to production environments.

> [!CAUTION]
> **Never commit secrets to source control!** Use Azure Key Vault, environment variables, or other secure secret management solutions.

## Table of Contents

1. [Secret Management](#secret-management)
2. [HTTPS Requirements](#https-requirements)
3. [Configuration Best Practices](#configuration-best-practices)
4. [CORS Configuration](#cors-configuration)
5. [Monitoring and Logging](#monitoring-and-logging)
6. [Performance Optimization](#performance-optimization)
7. [Deployment Checklist](#deployment-checklist)

---

## Secret Management

### Azure Key Vault Integration

**Recommended for Production**

#### 1. Install Required Packages

```bash
dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets
dotnet add package Azure.Identity
```

#### 2. Configure Key Vault in Program.cs

```csharp
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add Azure Key Vault
if (!builder.Environment.IsDevelopment())
{
    var keyVaultEndpoint = new Uri(builder.Configuration["KeyVaultUri"]);
    builder.Configuration.AddAzureKeyVault(
        keyVaultEndpoint,
        new DefaultAzureCredential()
    );
}

// Configure Primus Identity (secrets now loaded from Key Vault)
builder.Services.AddPrimusIdentity(options =>
{
    builder.Configuration.GetSection("PrimusIdentity").Bind(options);
});
```

#### 3. Store Secrets in Key Vault

```bash
# Azure CLI
az keyvault secret set \
  --vault-name "your-keyvault" \
  --name "PrimusIdentity--Issuers--0--Secret" \
  --value "your-production-secret-key"

# PowerShell
Set-AzKeyVaultSecret `
  -VaultName "your-keyvault" `
  -Name "PrimusIdentity--Issuers--0--Secret" `
  -SecretValue (ConvertTo-SecureString "your-production-secret-key" -AsPlainText -Force)
```

> [!NOTE]
> Key Vault secret names use `--` instead of `:` for nested configuration.

#### 4. Grant Access to Key Vault

```bash
# Grant your app's managed identity access
az keyvault set-policy \
  --name "your-keyvault" \
  --object-id "<YOUR_APP_MANAGED_IDENTITY_ID>" \
  --secret-permissions get list
```

---

### User Secrets (Development Only)

```bash
# Initialize user secrets
dotnet user-secrets init

# Set secrets
dotnet user-secrets set "PrimusIdentity:Issuers:0:Secret" "dev-secret-key"
dotnet user-secrets set "PrimusIdentity:Issuers:1:Secret" "another-dev-secret"
```

---

### Environment Variables

For non-Azure deployments:

```bash
# Linux/Mac
export PrimusIdentity__Issuers__0__Secret="production-secret"

# Windows PowerShell
$env:PrimusIdentity__Issuers__0__Secret="production-secret"

# Docker
docker run -e PrimusIdentity__Issuers__0__Secret="production-secret" your-image
```

```csharp
// Load from environment variables
builder.Configuration.AddEnvironmentVariables();
```

---

## HTTPS Requirements

### Enable HTTPS Metadata Validation

```json
{
  "PrimusIdentity": {
    "RequireHttpsMetadata": true  // ✅ Always true in production
  }
}
```

```csharp
builder.Services.AddPrimusIdentity(options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
});
```

### Configure HTTPS Redirection

```csharp
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();  // ✅ Redirect HTTP to HTTPS
    app.UseHsts();  // ✅ Enable HTTP Strict Transport Security
}

app.UseAuthentication();
app.UseAuthorization();
```

### SSL Certificate Setup

#### Azure App Service
- Automatically provides SSL certificate
- Configure custom domain in Azure Portal
- Enable "HTTPS Only" in App Service settings

#### Self-Hosted
```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(IPAddress.Any, 443, listenOptions =>
    {
        listenOptions.UseHttps("certificate.pfx", "certificate-password");
    });
});
```

---

## Configuration Best Practices

### Environment-Specific Configuration

**appsettings.json** (Base configuration)
```json
{
  "PrimusIdentity": {
    "ValidateLifetime": true,
    "ClockSkew": "00:05:00"
  }
}
```

**appsettings.Development.json**
```json
{
  "PrimusIdentity": {
    "RequireHttpsMetadata": false,
    "Issuers": [
      {
        "Name": "LocalAuth",
        "Type": "Jwt",
        "Issuer": "https://localhost:5265",
        "Secret": "dev-secret-key",
        "Audiences": ["api://dev-app-id"]
      }
    ]
  }
}
```

**appsettings.Production.json**
```json
{
  "PrimusIdentity": {
    "RequireHttpsMetadata": true,
    "Issuers": [
      {
        "Name": "AzureAD",
        "Type": "Oidc",
        "Issuer": "https://login.microsoftonline.com/PROD_TENANT/v2.0",
        "Authority": "https://login.microsoftonline.com/PROD_TENANT/v2.0",
        "Audiences": ["api://prod-app-id"]
      }
    ]
  },
  "Logging": {
    "LogLevel": {
      "Microsoft.AspNetCore.Authentication": "Warning"
    }
  }
}
```

### Validate Configuration on Startup

```csharp
builder.Services.AddOptions<PrimusIdentityOptions>()
    .Bind(builder.Configuration.GetSection("PrimusIdentity"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

---

## CORS Configuration

### Configure CORS for Frontend Apps

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("ProductionCors", policy =>
    {
        policy.WithOrigins(
                "https://yourdomain.com",
                "https://app.yourdomain.com"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors("ProductionCors");
app.UseAuthentication();
app.UseAuthorization();
```

### Environment-Specific CORS

```csharp
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("DevCors", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });
}
else
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("ProdCors", policy =>
        {
            policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>())
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
    });
}
```

---

## Monitoring and Logging

### Application Insights Integration

```bash
dotnet add package Microsoft.ApplicationInsights.AspNetCore
```

```csharp
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
});
```

### Structured Logging

```csharp
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    
    try
    {
        await next();
        
        if (context.User.Identity?.IsAuthenticated == true)
        {
            logger.LogInformation(
                "Authenticated request: {Method} {Path} by {UserId}",
                context.Request.Method,
                context.Request.Path,
                context.User.FindFirst("sub")?.Value
            );
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Authentication error for {Path}", context.Request.Path);
        throw;
    }
});
```

### Log Authentication Failures

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore.Authentication": "Warning",
      "PrimusSaaS.Identity.Validator": "Information"
    }
  }
}
```

### Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddCheck("authentication", () =>
    {
        // Verify authentication configuration is valid
        var config = builder.Configuration.GetSection("PrimusIdentity");
        return config.Exists() 
            ? HealthCheckResult.Healthy() 
            : HealthCheckResult.Unhealthy("PrimusIdentity configuration missing");
    });

app.MapHealthChecks("/health");
```

---

## Performance Optimization

### JWKS Caching

```csharp
builder.Services.AddPrimusIdentity(options =>
{
    options.JwksCacheTtl = TimeSpan.FromHours(24);  // Cache JWKS for 24 hours
});
```

### Response Caching

```csharp
builder.Services.AddResponseCaching();

app.UseResponseCaching();
app.UseAuthentication();
app.UseAuthorization();
```

### Compression

```csharp
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

app.UseResponseCompression();
```

---

## Deployment Checklist

### Pre-Deployment

- [ ] **Secrets Management**
  - [ ] All secrets stored in Azure Key Vault or secure vault
  - [ ] No secrets in appsettings.json or source control
  - [ ] Managed identity configured for Key Vault access
  
- [ ] **HTTPS Configuration**
  - [ ] `RequireHttpsMetadata: true` in production config
  - [ ] SSL certificate configured
  - [ ] HTTPS redirection enabled
  - [ ] HSTS enabled

- [ ] **Configuration Validation**
  - [ ] Environment-specific configs tested
  - [ ] Issuer URLs point to production identity providers
  - [ ] Audience values match production app registrations
  - [ ] Clock skew set appropriately (5-10 minutes)

- [ ] **CORS Configuration**
  - [ ] Allowed origins limited to production domains
  - [ ] No `AllowAnyOrigin()` in production
  - [ ] Credentials enabled if needed

- [ ] **Logging**
  - [ ] Application Insights or logging provider configured
  - [ ] Log levels appropriate for production
  - [ ] Health check endpoint exposed
  - [ ] Authentication failures logged

- [ ] **Testing**
  - [ ] Integration tests pass with production-like config
  - [ ] Token validation tested with production issuers
  - [ ] CORS tested from production frontend domains

### Post-Deployment

- [ ] **Monitoring**
  - [ ] Health check endpoint returning healthy status
  - [ ] Authentication logs showing successful validations
  - [ ] No unexpected 401/403 errors
  - [ ] Application Insights showing telemetry

- [ ] **Security Verification**
  - [ ] HTTPS enforced (no HTTP access)
  - [ ] Secrets not exposed in logs or error messages
  - [ ] CORS working correctly (no CORS errors in browser)

- [ ] **Performance**
  - [ ] JWKS caching working (check logs for cache hits)
  - [ ] Response times acceptable
  - [ ] No memory leaks or excessive CPU usage

---

## Troubleshooting Production Issues

### Enable Detailed Logging Temporarily

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.AspNetCore.Authentication": "Debug",
      "PrimusSaaS.Identity.Validator": "Debug"
    }
  }
}
```

> [!WARNING]
> Revert to `Warning` or `Information` after troubleshooting to avoid excessive log volume.

### Check Key Vault Access

```bash
# Verify managed identity has access
az keyvault secret show \
  --vault-name "your-keyvault" \
  --name "PrimusIdentity--Issuers--0--Secret" \
  --query "value"
```

### Verify Configuration Loading

Add temporary debug endpoint (remove after troubleshooting):

```csharp
app.MapGet("/debug/config", (IConfiguration config) =>
{
    return new
    {
        issuersCount = config.GetSection("PrimusIdentity:Issuers").GetChildren().Count(),
        requireHttps = config["PrimusIdentity:RequireHttpsMetadata"],
        // Don't expose actual secrets!
        secretsConfigured = config["PrimusIdentity:Issuers:0:Secret"] != null
    };
}).RequireAuthorization();  // Protect this endpoint!
```

---

## Additional Resources

- [TOKEN_GENERATION_GUIDE.md](./TOKEN_GENERATION_GUIDE.md) - Token generation examples
- [ERROR_REFERENCE.md](./ERROR_REFERENCE.md) - Troubleshooting validation errors
- [Azure Key Vault Documentation](https://docs.microsoft.com/azure/key-vault/)
- [ASP.NET Core Security Best Practices](https://docs.microsoft.com/aspnet/core/security/)

---

**Need Help?**
- GitHub Issues: https://github.com/akkikhan/Primus-SaaS/issues
- Documentation: https://portal.primus-saas.com/docs
