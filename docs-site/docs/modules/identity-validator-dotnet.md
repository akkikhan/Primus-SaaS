# Identity Validator - .NET Quick Start

Multi-issuer JWT/OIDC token validator for ASP.NET Core applications.

## Installation

```bash
dotnet add package PrimusSaaS.Identity.Validator
```

## Configuration

Add to your `Program.cs`:

```csharp
using PrimusSaaS.Identity.Validator;

var builder = WebApplication.CreateBuilder(args);

// Add Primus Identity validation
builder.Services.AddPrimusIdentity(options =>
{
    options.Issuers = new()
    {
        new IssuerConfig
        {
            Name = "AzureAD",
            Type = IssuerType.Oidc,
            Issuer = "https://login.microsoftonline.com/<TENANT_ID>/v2.0",
            Authority = "https://login.microsoftonline.com/<TENANT_ID>/v2.0",
            Audiences = new List<string> { "api://your-api-id" }
        }
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
```

## Protect Endpoints

```csharp
[ApiController]
[Route("api/[controller]")]
public class SecureController : ControllerBase
{
    [HttpGet]
    [Authorize]
    public IActionResult GetData()
    {
        var user = HttpContext.GetPrimusUser();
        return Ok(new { userId = user?.UserId, email = user?.Email });
    }
}
```

## Next Steps

- [Full Configuration Guide](./identity-configuration)
- [Token Generation](./identity-token-generation)
- [Error Reference](./identity-error-reference)
