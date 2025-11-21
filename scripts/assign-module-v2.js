const axios = require('axios');

const baseUrl = 'http://localhost:5267/api';
const appId = 9; // From our E2E test

async function assignModule() {
    console.log('='.repeat(60));
    console.log('🔗 ASSIGNING MODULE TO APPLICATION');
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

        // Get modules
        console.log('Step 2: Fetching modules...');
        const modulesResponse = await axios.get(`${baseUrl}/modules`, { headers });
        const modules = modulesResponse.data;

        // Find IdentityValidator module
        const module = modules.find(m => m.moduleKey === 'identity-validator' || m.name === 'IdentityValidator');

        if (!module) {
            console.log('❌ IdentityValidator module not found');
            return;
        }
        console.log(`✅ Found Module: ${module.name} (ID: ${module.id})`);

        // Get versions to find the one for NodeJS
        console.log('Step 3: Fetching module versions...');
        const versionsResponse = await axios.get(`${baseUrl}/modules/${module.id}/versions`, { headers });
        const versions = versionsResponse.data;

        const nodeVersion = versions.find(v => {
            try {
                const stacks = JSON.parse(v.supportedStacksJson || '[]');
                return stacks.includes('NodeJS');
            } catch (e) { return false; }
        });

        if (!nodeVersion) {
            console.log('❌ No NodeJS version found for this module');
            // Try to find any version
            if (versions.length > 0) {
                console.log(`⚠️ Using first available version: ${versions[0].version}`);
                // Use the first one
            } else {
                return;
            }
        } else {
            console.log(`✅ Found NodeJS Version: ${nodeVersion.version} (ID: ${nodeVersion.id})`);
        }

        const versionToAssign = nodeVersion || versions[0];

        // Assign module to application
        console.log(`Step 4: Assigning module to Application ${appId}...`);

        try {
            await axios.post(
                `${baseUrl}/applications/${appId}/modules`,
                {
                    moduleId: module.id,
                    versionId: versionToAssign.id
                },
                { headers }
            );
            console.log(`✅ Module assigned successfully!\n`);
        } catch (error) {
            if (error.response?.data?.message?.includes('already assigned') || error.response?.status === 400) {
                console.log(`⚠️ Module likely already assigned (Status: ${error.response.status})`);
            } else {
                throw error;
            }
        }

        // Test documentation generation
        console.log('Step 5: Generating documentation...');
        // Note: Controller is DocumentationController, route is api/documentation/{id}
        const docUrl = `${baseUrl}/documentation/${appId}`;
        console.log(`GET ${docUrl}`);

        const docResponse = await axios.get(docUrl, { headers });

        const doc = docResponse.data;
        console.log(`✅ Documentation generated!`);
        console.log(`   Application: ${doc.applicationName}`);
        console.log(`   Modules: ${doc.modules ? doc.modules.length : 0}`);

        if (doc.modules) {
            doc.modules.forEach(m => {
                console.log(`     - ${m.moduleName} v${m.version}`);
                console.log(`       Steps: ${m.integrationSteps ? m.integrationSteps.length : 0}`);
            });
        }
        console.log('');

        console.log('='.repeat(60));
        console.log('✅ SUCCESS! Documentation generation working!');
        console.log('='.repeat(60));

    } catch (error) {
        console.error('❌ Error:', error.response?.data || error.message);
        if (error.response) {
            console.error('Status:', error.response.status);
        }
        process.exit(1);
    }
}

assignModule();
