# Live Demo Instructions: Primus Identity Validator with Azure AD & Auth0

This guide outlines the steps to create a new .NET Web API application and configure it with `PrimusSaaS.Identity.Validator` to support both Azure AD and Auth0 authentication.

## Prerequisites
- .NET 8.0 SDK installed
- Access to `PrimusSaaS.Identity.Validator` NuGet package (configured via local source or NuGet feed)

## Step 1: Create a New Web API Project
Run the following command to create a new Web API project:
```bash
dotnet new webapi -n LiveDemoApi
cd LiveDemoApi
```

## Step 2: Add the Identity + Logging Packages
Add the `PrimusSaaS.Identity.Validator` and `PrimusSaaS.Logging` packages to your project:
```bash
dotnet add package PrimusSaaS.Identity.Validator --version 1.3.5
dotnet add package PrimusSaaS.Logging --version 1.2.3
```
*Note: Ensure your `nuget.config` is set up to find the package if it's hosted locally.*

## Step 3: Configure `Program.cs`
Open `Program.cs` and make the following changes:

1.  **Add Namespace**:
    ```csharp
    using PrimusSaaS.Identity.Validator;
    using PrimusSaaS.Logging.Extensions;
    ```

2.  **Register Services**:
    Add the Primus Identity services before `builder.Build()`:
    ```csharp
    // Structured logging with Primus logging
    builder.Logging.ClearProviders();
    builder.Logging.AddPrimus(options =>
    {
        builder.Configuration.GetSection("PrimusLogging").Bind(options);
    });

    // Configure Primus Identity from appsettings
    builder.Services.AddPrimusIdentity(options =>
    {
        builder.Configuration.GetSection("PrimusIdentity").Bind(options);
    });

    // Add standard Authorization services
    builder.Services.AddAuthorization();
    ```

3.  **Add Middleware**:
    Add the authentication and authorization middleware in the correct order (after `UseHttpsRedirection`):
    ```csharp
    app.UsePrimusLogging(); // request logging + correlation IDs
    app.UseAuthentication();
    app.UseAuthorization();
    ```

4.  **Optional Diagnostics**:
    ```csharp
    app.MapPrimusIdentityDiagnostics();
    ```

## Step 4: Configure `appsettings.json`
Open `appsettings.json` and add the `PrimusIdentity` section to configure your issuers (Azure AD and Auth0), plus `PrimusLogging` for structured logs.

```json
{
  "Logging": { ... },
  "PrimusLogging": {
    "MinimumLevel": "Information",
    "RedactSensitiveData": true,
    "EnableScopes": true,
    "Targets": {
      "Console": { "Enabled": true },
      "File": {
        "Enabled": true,
        "Path": "logs/app.log",
        "RollingInterval": "Day"
      }
    }
  },
  "AllowedHosts": "*",
  "PrimusIdentity": {
    "Issuers": [
      {
        "Name": "AzureAD",
        "Type": "Oidc",
        "Issuer": "https://login.microsoftonline.com/<TENANT_ID>/v2.0",
        "Authority": "https://login.microsoftonline.com/<TENANT_ID>/v2.0",
        "Audiences": [ "api://your-azure-api-id" ]
      },
      {
        "Name": "Auth0",
        "Type": "Oidc",
        "Issuer": "https://<YOUR_AUTH0_DOMAIN>/",
        "Authority": "https://<YOUR_AUTH0_DOMAIN>/",
        "Audiences": [ "https://your-auth0-api-identifier" ]
      }
    ],
    "RequireHttpsMetadata": true
  }
}
```
*Replace `<TENANT_ID>`, `<YOUR_AUTH0_DOMAIN>`, and Audience values with your actual configuration.*

## Step 5: Run the Application
Run the application:
```bash
dotnet run
```
Navigate to `/swagger` to see the API.
Access `/primus/diagnostics` to see the loaded configuration (if enabled).
Try accessing a protected endpoint (e.g., `/weatherforecast` if you added `[Authorize]`) with a valid token from either provider.
