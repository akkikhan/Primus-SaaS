const axios = require('axios');

const baseUrl = 'http://localhost:5267/api';

async function setupModules() {
    console.log('='.repeat(60));
    console.log('🔧 COMPLETE MODULE SETUP');
    console.log('='.repeat(60));
    console.log('');

    try {
        // Login
        console.log('Step 1: Logging in...');
        const loginResponse = await axios.post(`${baseUrl}/auth/login`, {
            email: 'admin@primussaas.com',
            password: 'Admin123!'
        });

        const { token } = loginResponse.data;
        console.log(`✅ Logged in\n`);

        const headers = {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
        };

        // Check existing modules
        console.log('Step 2: Checking existing modules...');
        const modulesResponse = await axios.get(`${baseUrl}/modules`, { headers });
        let module = modulesResponse.data.find(m => m.moduleKey === 'identity-validator');

        if (!module) {
            console.log('Creating new module...');
            const createResponse = await axios.post(`${baseUrl}/modules`, {
                name: 'IdentityValidator',
                moduleKey: 'identity-validator',
                description: 'Official SDK for validating JWT tokens issued by Primus SaaS Portal'
            }, { headers });
            module = createResponse.data;
            console.log(`✅ Created module (ID: ${module.id})\n`);
        } else {
            console.log(`✅ Module exists (ID: ${module.id})\n`);
        }

        // Check module versions
        console.log('Step 3: Checking module versions...');
        const versionsResponse = await axios.get(`${baseUrl}/modules/${module.id}/versions`, { headers });
        let hasNodeVersion = versionsResponse.data.some(v => {
            const stacks = JSON.parse(v.supportedStacksJson || '[]');
            return stacks.includes('NodeJS');
        });

        let hasDotNetVersion = versionsResponse.data.some(v => {
            const stacks = JSON.parse(v.supportedStacksJson || '[]');
            return stacks.includes('DotNet');
        });

        // Create NodeJS version if needed
        if (!hasNodeVersion) {
            console.log('Creating NodeJS version...');
            const nodeVersion = {
                version: '1.0.0',
                releaseNotes: 'Initial stable release with Local JWT and Azure AD validation support.',
                changelog: 'First release',
                isBreakingChange: false,
                supportedStacks: ['NodeJS'],
                integrationSteps: [
                    'Install the package: npm install primus-identity-validator',
                    'Import the middleware: const { primusIdentityMiddleware } = require(\'primus-identity-validator\');',
                    'Configure with your credentials',
                    'Protect your routes: app.use(\'/api/protected\', primusAuth);',
                    'Access authenticated user via req.primusUser'
                ],
                demoCode: `const express = require('express');
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

app.listen(3000);`
            };

            await axios.post(`${baseUrl}/modules/${module.id}/versions`, nodeVersion, { headers });
            console.log(`✅ Created NodeJS version 1.0.0\n`);
        } else {
            console.log(`✅ NodeJS version exists\n`);
        }

        // Create DotNet version if needed
        if (!hasDotNetVersion) {
            console.log('Creating DotNet version...');
            const dotnetVersion = {
                version: '1.0.0',
                releaseNotes: 'Initial stable release with JWT authentication for ASP.NET Core 7.0+.',
                changelog: 'First release',
                isBreakingChange: false,
                supportedStacks: ['DotNet'],
                integrationSteps: [
                    'Install the package: dotnet add package PrimusSaaS.Identity.Validator',
                    'Add to Program.cs: using PrimusSaaS.Identity.Validator;',
                    'Configure services: builder.Services.AddPrimusIdentity(...);',
                    'Enable middleware: app.UseAuthentication(); app.UseAuthorization();',
                    'Protect controllers with [Authorize] attribute',
                    'Access user via HttpContext.GetPrimusUser()'
                ],
                demoCode: `using PrimusSaaS.Identity.Validator;

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
app.Run();`
            };

            await axios.post(`${baseUrl}/modules/${module.id}/versions`, dotnetVersion, { headers });
            console.log(`✅ Created DotNet version 1.0.0\n`);
        } else {
            console.log(`✅ DotNet version exists\n`);
        }

        // Assign to test application
        const appId = 9;
        console.log(`Step 4: Assigning module to Application ${appId}...`);

        try {
            // Get the latest version for NodeJS
            const versionsResp = await axios.get(`${baseUrl}/modules/${module.id}/versions`, { headers });
            const nodeVersion = versionsResp.data.find(v => {
                const stacks = JSON.parse(v.supportedStacksJson || '[]');
                return stacks.includes('NodeJS');
            });

            await axios.post(
                `${baseUrl}/applications/${appId}/modules`,
                {
                    moduleId: module.id,
                    versionId: nodeVersion.id
                },
                { headers }
            );
            console.log(`✅ Module assigned!\n`);
        } catch (assignError) {
            if (assignError.response?.status === 400) {
                console.log(`✅ Module already assigned\n`);
            } else {
                throw assignError;
            }
        }

        // Test documentation generation
        console.log('Step 5: Testing documentation generation...');
        const docResponse = await axios.get(
            `${baseUrl}/applications/${appId}/documentation`,
            { headers }
        );

        const doc = docResponse.data;
        console.log(`✅ Documentation generated successfully!`);
        console.log(`   Application: ${doc.applicationName}`);
        console.log(`   Client ID: ${doc.primusClientId}`);
        console.log(`   Modules: ${doc.modules.length}`);
        doc.modules.forEach(m => {
            console.log(`     • ${m.moduleName} v${m.version}`);
            console.log(`       - Integration Steps: ${m.integrationSteps?.length || 0}`);
            console.log(`       - Demo Code Length: ${m.demoCode?.length || 0} chars`);
        });
        console.log('');

        console.log('='.repeat(60));
        console.log('✅ SETUP COMPLETE!');
        console.log('='.repeat(60));
        console.log('');
        console.log('🎉 Next Steps:');
        console.log('   1. Portal documentation page now works');
        console.log('   2. Users can export docs (PDF, MD, JSON)');
        console.log('   3. Ready to publish SDKs to npm/NuGet');

        return { moduleId: module.id, documentation: doc };

    } catch (error) {
        console.error('❌ Error:', error.response?.data || error.message);
        if (error.response?.data) {
            console.error('Response data:', JSON.stringify(error.response.data, null, 2));
        }
        throw error;
    }
}

setupModules()
    .then(() => process.exit(0))
    .catch(() => process.exit(1));
