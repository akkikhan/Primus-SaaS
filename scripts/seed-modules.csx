using System.Net.Http;
using System.Text;
using System.Text.Json;

var baseUrl = "http://localhost:5267/api";

// Login to get admin token
var loginPayload = new
{
    email = "admin@primussaas.com",
    password = "Admin123!"
};

var client = new HttpClient();
var loginResponse = await client.PostAsync(
    $"{baseUrl}/auth/login",
    new StringContent(JsonSerializer.Serialize(loginPayload), Encoding.UTF8, "application/json")
);

var loginResult = await loginResponse.Content.ReadAsStringAsync();
var loginData = JsonSerializer.Deserialize<JsonElement>(loginResult);
var token = loginData.GetProperty("token").GetString();

Console.WriteLine($"✅ Logged in successfully");
Console.WriteLine($"Token: {token?.Substring(0, 20)}...\n");

// Set authorization header
client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

// Create IdentityValidator module for NodeJS
var nodeModule = new
{
    name = "IdentityValidator",
    version = "1.0.0",
    stack = "NodeJS",
    description = "Official Node.js SDK for validating JWT tokens issued by Primus SaaS Portal. Provides Express middleware for seamless authentication integration.",
    releaseNotes = "Initial stable release with Local JWT and Azure AD validation support.",
    isBreakingChange = false,
    integrationSteps = new[]
    {
        "Install the package: npm install primus-identity-validator",
        "Import the middleware: const { primusIdentityMiddleware } = require('primus-identity-validator');",
        "Configure with your credentials: const primusAuth = primusIdentityMiddleware({ portalUrl, clientId, clientSecret, jwtSecret, mode: 'Local' });",
        "Protect your routes: app.use('/api/protected', primusAuth);",
        "Access authenticated user via req.primusUser in your route handlers"
    },
    codeSnippets = new Dictionary<string, string>
    {
        ["server.js"] = @"const express = require('express');
const { primusIdentityMiddleware } = require('primus-identity-validator');

const app = express();

const primusAuth = primusIdentityMiddleware({
    portalUrl: 'YOUR_PORTAL_URL',
    clientId: 'YOUR_CLIENT_ID',
    clientSecret: 'YOUR_CLIENT_SECRET',
    jwtSecret: 'YOUR_CLIENT_SECRET',
    mode: 'Local'
});

app.get('/api/protected', primusAuth, (req, res) => {
    res.json({ user: req.primusUser });
});

app.listen(3000);",
        ["package.json"] = @"{
  ""name"": ""my-app"",
  ""dependencies"": {
    ""express"": ""^4.18.0"",
    ""primus-identity-validator"": ""^1.0.0""
  }
}"
    }
};

var nodeResponse = await client.PostAsync(
    $"{baseUrl}/modules",
    new StringContent(JsonSerializer.Serialize(nodeModule), Encoding.UTF8, "application/json")
);

if (nodeResponse.IsSuccessStatusCode)
{
    var result = await nodeResponse.Content.ReadAsStringAsync();
    Console.WriteLine("✅ Created NodeJS IdentityValidator module");
    Console.WriteLine(result);
}
else
{
    Console.WriteLine($"❌ Failed to create NodeJS module: {await nodeResponse.Content.ReadAsStringAsync()}");
}

Console.WriteLine();

// Create IdentityValidator module for DotNet
var dotnetModule = new
{
    name = "IdentityValidator",
    version = "1.0.0",
    stack = "DotNet",
    description = "Official .NET SDK for validating JWT tokens issued by Primus SaaS Portal. Provides middleware and extensions for ASP.NET Core applications.",
    releaseNotes = "Initial stable release with JWT authentication support for ASP.NET Core 7.0+.",
    isBreakingChange = false,
    integrationSteps = new[]
    {
        "Install the package: dotnet add package PrimusSaaS.Identity.Validator",
        "Add to Program.cs: using PrimusSaaS.Identity.Validator;",
        "Configure services: builder.Services.AddPrimusIdentity(options => { options.PortalUrl = \"...\"; options.ClientId = \"...\"; options.ClientSecret = \"...\"; options.JwtSecret = \"...\"; });",
        "Enable middleware: app.UseAuthentication(); app.UseAuthorization();",
        "Protect controllers with [Authorize] attribute",
        "Access user via HttpContext.GetPrimusUser() in your controllers"
    },
    codeSnippets = new Dictionary<string, string>
    {
        ["Program.cs"] = @"using PrimusSaaS.Identity.Validator;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPrimusIdentity(options =>
{
    options.PortalUrl = ""YOUR_PORTAL_URL"";
    options.ClientId = ""YOUR_CLIENT_ID"";
    options.ClientSecret = ""YOUR_CLIENT_SECRET"";
    options.JwtSecret = ""YOUR_CLIENT_SECRET"";
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();",
        ["SecureController.cs"] = @"using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrimusSaaS.Identity.Validator;

[ApiController]
[Route(""api/[controller]"")]
public class SecureController : ControllerBase
{
    [HttpGet]
    [Authorize]
    public IActionResult Get()
    {
        var user = HttpContext.GetPrimusUser();
        return Ok(new { user });
    }
}",
        ["appsettings.json"] = @"{
  ""Primus"": {
    ""PortalUrl"": ""YOUR_PORTAL_URL"",
    ""ClientId"": ""YOUR_CLIENT_ID"",
    ""ClientSecret"": ""YOUR_CLIENT_SECRET""
  }
}"
    }
};

var dotnetResponse = await client.PostAsync(
    $"{baseUrl}/modules",
    new StringContent(JsonSerializer.Serialize(dotnetModule), Encoding.UTF8, "application/json")
);

if (dotnetResponse.IsSuccessStatusCode)
{
    var result = await dotnetResponse.Content.ReadAsStringAsync();
    Console.WriteLine("✅ Created DotNet IdentityValidator module");
    Console.WriteLine(result);
}
else
{
    Console.WriteLine($"❌ Failed to create DotNet module: {await dotnetResponse.Content.ReadAsStringAsync()}");
}

Console.WriteLine("\n" + new string('=', 60));
Console.WriteLine("✅ Module seeding complete!");
Console.WriteLine(new string('=', 60));
