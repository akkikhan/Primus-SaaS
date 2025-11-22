# Primus SaaS .NET SDK Example

This is an example ASP.NET Core Web API demonstrating how to use the **PrimusSaaS.Identity.Validator** package to secure your API with JWT/OIDC authentication from your own identity providers (e.g., Azure AD + LocalAuth). Primus does not issue tokens or sit in the runtime path.

## Features Demonstrated

- ✅ JWT Bearer authentication configuration
- ✅ Protected endpoints requiring authentication
- ✅ Role-based authorization (RBAC)
- ✅ User information extraction from JWT tokens
- ✅ Public and private endpoint examples
- ✅ Swagger integration for API testing

## Prerequisites

- .NET 7.0 SDK or later
- Identity provider credentials (e.g., Azure AD app registration, local issuer secret/JWKS)

## Getting Started

### 1. Install Dependencies

The project already references the PrimusSaaS.Identity.Validator package via project reference:

```bash
cd PrimusSaaS.Example.Api
dotnet restore
```

### 2. Configure Application Settings

Update `appsettings.Development.json` with your issuer configurations:

```json
{
  "PrimusIdentity": {
    "Issuers": [
      {
        "Name": "AzureAD",
        "Type": "Oidc",
        "Issuer": "https://login.microsoftonline.com/<TENANT_ID>/v2.0",
        "Authority": "https://login.microsoftonline.com/<TENANT_ID>/v2.0",
        "Audiences": [ "api://your-api-id" ]
      },
      {
        "Name": "LocalAuth",
        "Type": "Jwt",
        "Issuer": "https://auth.yourcompany.com",
        "Secret": "your-local-secret",
        "Audiences": [ "api://your-api-id" ]
      }
    ]
  }
}
```

### 3. Run the Application

```bash
dotnet run
```

The API will start on `https://localhost:7xxx` (check console output for exact port).

### 4. Access Swagger UI

Navigate to `https://localhost:7xxx/swagger` to see the interactive API documentation.

## API Endpoints

### Public Endpoint

**GET** `/api/public` - No authentication required

```bash
curl https://localhost:7001/api/public
```

Response:
```json
{
  "message": "This is a public endpoint accessible without authentication"
}
```

### Protected Endpoint

**GET** `/api/protected` - Requires authentication

```bash
curl -H "Authorization: Bearer YOUR_JWT_TOKEN" https://localhost:7001/api/protected
```

Response:
```json
{
  "message": "This endpoint requires authentication",
  "user": {
    "userId": "123",
    "email": "user@example.com",
    "name": "John Doe",
    "roles": ["User"]
  }
}
```

### Admin Endpoint

**GET** `/api/admin` - Requires Admin role

```bash
curl -H "Authorization: Bearer YOUR_JWT_TOKEN" https://localhost:7001/api/admin
```

### Weather Endpoints

**GET** `/api/weather` - Requires authentication

Returns 5-day weather forecast. Demonstrates authenticated endpoint with user logging.

**GET** `/api/weather/extended` - Requires Admin or Manager role

Returns 14-day weather forecast. Demonstrates role-based authorization.

## Code Walkthrough

### Program.cs - Configuration

```csharp
// Add Primus Identity authentication
builder.Services.AddPrimusIdentity(options =>
{
    builder.Configuration.GetSection("PrimusIdentity").Bind(options);
});

// Enable authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();
```

### Minimal API Endpoints

```csharp
// Protected endpoint with [Authorize] attribute
app.MapGet("/api/protected", [Authorize] (HttpContext context) =>
{
    // Extract user information from JWT
    var user = context.GetPrimusUser();
    return new { message = "Protected data", user };
});

// Role-based endpoint
app.MapGet("/api/admin", [Authorize(Roles = "Admin")] (HttpContext context) =>
{
    var user = context.GetPrimusUser();
    return new { message = "Admin data", user };
});
```

### Controller Endpoints

```csharp
[ApiController]
[Route("api/[controller]")]
public class WeatherController : ControllerBase
{
    [HttpGet]
    [Authorize] // Requires authentication
    public ActionResult<IEnumerable<WeatherForecast>> GetWeather()
    {
        // Extract user from HttpContext
        var user = HttpContext.GetPrimusUser();
        
        _logger.LogInformation("User {UserId} requested data", user?.UserId);
        
        // Return weather data
    }
    
    [HttpGet("extended")]
    [Authorize(Roles = "Admin,Manager")] // Requires specific roles
    public ActionResult<IEnumerable<WeatherForecast>> GetExtendedWeather()
    {
        var user = HttpContext.GetPrimusUser();
        // Return extended data
    }
}
```


## Testing with JWT Tokens

### Option 1: Using Swagger UI

1. Navigate to `https://localhost:7xxx/swagger`
2. Click the "Authorize" button
3. Enter your JWT token in the format: `Bearer YOUR_TOKEN`
4. Test protected endpoints directly in Swagger

### Option 2: Using curl

```bash
# Get a JWT access token from your IdP (e.g., Azure AD or LocalAuth)
TOKEN="eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9..."

# Test protected endpoint
curl -H "Authorization: Bearer $TOKEN" https://localhost:7001/api/protected

# Test admin endpoint (requires Admin role in token)
curl -H "Authorization: Bearer $TOKEN" https://localhost:7001/api/admin
```

### Option 3: Creating Test Tokens

For development, you can create test JWT tokens using your local issuer secret:

```csharp
// Token payload should include:
{
  "sub": "user-id-123",
  "email": "test@example.com",
  "name": "Test User",
  "role": ["Admin", "User"],
  "iss": "https://login.microsoftonline.com/<TENANT_ID>/v2.0",
  "aud": "api://your-api-id",
  "exp": 1700000000
}
```

You can use [jwt.io](https://jwt.io) to create test tokens with your JwtSecret.

## Project Structure

```
PrimusSaaS.Example.Api/
├── Controllers/
│   └── WeatherController.cs    # Controller-based endpoints
├── Program.cs                   # App configuration and minimal API endpoints
├── appsettings.json            # Production configuration template
├── appsettings.Development.json # Development configuration
└── PrimusSaaS.Example.Api.csproj # Project file with SDK reference
```


## Key Concepts

### 1. Adding Primus Identity

```csharp
builder.Services.AddPrimusIdentity(options => { /* config */ });
```

This extension method configures JWT Bearer authentication with your issuer settings.

### 2. User Extraction

```csharp
var user = HttpContext.GetPrimusUser();
```

The `GetPrimusUser()` extension method extracts user information from the authenticated JWT token.

### 3. Authorization Attributes

- `[Authorize]` - Requires any authenticated user
- `[Authorize(Roles = "Admin")]` - Requires specific role
- `[Authorize(Roles = "Admin,Manager")]` - Requires any of the listed roles
- `[AllowAnonymous]` - Allows unauthenticated access (overrides [Authorize])

## Troubleshooting

### 401 Unauthorized

- Verify JWT token is included in Authorization header
- Check token hasn't expired
- Ensure JwtSecret matches the one used to sign the token
- Verify Issuer and Audience match configuration

### 403 Forbidden

- User is authenticated but lacks required role
- Check token includes required roles in "role" claim
- Verify role names match exactly (case-sensitive)

### Configuration Errors

- Ensure all required configuration values are set
- Check PortalUrl format (must be valid URL)
- Verify issuer URLs and audiences match your IdP configuration

## Next Steps

- Add custom authorization policies
- Implement refresh token handling
- Add rate limiting and throttling
- Integrate with database for user data
- Add comprehensive logging and monitoring

## Learn More

- [PrimusSaaS.Identity.Validator Documentation](../../sdk/dotnet/PrimusSaaS.Identity.Validator/README.md)
- [ASP.NET Core Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/)
- [JWT Bearer Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/jwt)

## License

MIT
