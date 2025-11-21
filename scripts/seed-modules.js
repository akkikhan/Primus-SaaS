const axios = require('axios');

const baseUrl = 'http://localhost:5267/api';

async function seedModules() {
    console.log('='.repeat(60));
    console.log('🌱 SEEDING MODULES');
    console.log('='.repeat(60));
    console.log('');

    try {
        // Login
        console.log('Step 1: Logging in as admin...');
        const loginResponse = await axios.post(`${baseUrl}/auth/login`, {
            email: 'admin@primussaas.com',
            password: 'Admin123!'
        });

        const { token } = loginResponse.data;
        console.log(`✅ Logged in successfully\n`);

        const headers = {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
        };

        // Create NodeJS module
        console.log('Step 2: Creating NodeJS IdentityValidator module...');
        const nodeModule = {
            name: 'IdentityValidator',
            moduleKey: 'identity-validator-nodejs',
            description: 'Official Node.js SDK for validating JWT tokens',
            stack: 'NodeJS',
            version: '1.0.0',
            releaseNotes: 'Initial stable release with Local JWT and Azure AD validation support.',
            isBreakingChange: false,
            integrationSteps: [
                'Install the package: npm install primus-identity-validator',
                'Import the middleware: const { primusIdentityMiddleware } = require(\'primus-identity-validator\');',
                'Configure with your credentials',
                'Protect your routes: app.use(\'/api/protected\', primusAuth);',
                'Access authenticated user via req.primusUser'
            ],
            codeSnippets: {
                'server.js': `const express = require('express');
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

app.listen(3000);`,
                'package.json': `{
  "name": "my-app",
  "dependencies": {
    "express": "^4.18.0",
    "primus-identity-validator": "^1.0.0"
  }
}`
            }
        };

        const nodeResponse = await axios.post(`${baseUrl}/modules`, nodeModule, { headers });
        console.log(`✅ Created NodeJS module (ID: ${nodeResponse.data.id})\n`);

        // Create DotNet module
        console.log('Step 3: Creating DotNet IdentityValidator module...');
        const dotnetModule = {
            name: 'IdentityValidator',
            moduleKey: 'identity-validator-dotnet',
            description: 'Official .NET SDK for validating JWT tokens',
            stack: 'DotNet',
            version: '1.0.0',
            releaseNotes: 'Initial stable release with JWT authentication for ASP.NET Core 7.0+.',
            isBreakingChange: false,
            integrationSteps: [
                'Install the package: dotnet add package PrimusSaaS.Identity.Validator',
                'Add to Program.cs: using PrimusSaaS.Identity.Validator;',
                'Configure services: builder.Services.AddPrimusIdentity(...);',
                'Enable middleware: app.UseAuthentication(); app.UseAuthorization();',
                'Protect controllers with [Authorize] attribute',
                'Access user via HttpContext.GetPrimusUser()'
            ],
            codeSnippets: {
                'Program.cs': `using PrimusSaaS.Identity.Validator;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPrimusIdentity(options =>
{
    options.PortalUrl = "YOUR_PORTAL_URL";
    options.ClientId = "YOUR_CLIENT_ID";
    options.ClientSecret = "YOUR_CLIENT_SECRET";
    options.JwtSecret = "YOUR_CLIENT_SECRET";
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();`,
                'SecureController.cs': `using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrimusSaaS.Identity.Validator;

[ApiController]
[Route("api/[controller]")]
public class SecureController : ControllerBase
{
    [HttpGet]
    [Authorize]
    public IActionResult Get()
    {
        var user = HttpContext.GetPrimusUser();
        return Ok(new { user });
    }
}`
            }
        };

        const dotnetResponse = await axios.post(`${baseUrl}/modules`, dotnetModule, { headers });
        console.log(`✅ Created DotNet module (ID: ${dotnetResponse.data.id})\n`);

        console.log('='.repeat(60));
        console.log('✅ Module seeding complete!');
        console.log('='.repeat(60));

        return {
            nodeModuleId: nodeResponse.data.id,
            dotnetModuleId: dotnetResponse.data.id
        };

    } catch (error) {
        console.error('❌ Error:', error.response?.data || error.message);
        throw error;
    }
}

// Run the seeding
seedModules()
    .then(result => {
        console.log('\nModule IDs:', result);
        process.exit(0);
    })
    .catch(error => {
        console.error('Seeding failed');
        process.exit(1);
    });
