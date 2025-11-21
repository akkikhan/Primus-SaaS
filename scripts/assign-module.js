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

        console.log(`✅ Found ${modules.length} modules:`);
        modules.forEach(m => {
            console.log(`   - ${m.name} (${m.stack}) - ID: ${m.id}`);
        });
        console.log('');

        // Find NodeJS module
        const nodeModule = modules.find(m => m.stack === 'NodeJS');

        if (!nodeModule) {
            console.log('❌ No NodeJS module found');
            return;
        }

        // Assign module to application
        console.log(`Step 3: Assigning ${nodeModule.name} to Application ${appId}...`);
        const assignResponse = await axios.post(
            `${baseUrl}/applications/${appId}/modules`,
            { moduleId: nodeModule.id },
            { headers }
        );

        console.log(`✅ Module assigned successfully!\n`);

        //  Test documentation generation
        console.log('Step 4: Generating documentation...');
        const docResponse = await axios.get(
            `${baseUrl}/applications/${appId}/documentation`,
            { headers }
        );

        const doc = docResponse.data;
        console.log(`✅ Documentation generated!`);
        console.log(`   Application: ${doc.applicationName}`);
        console.log(`   Modules: ${doc.modules.length}`);
        doc.modules.forEach(m => {
            console.log(`     - ${m.moduleName} v${m.version}`);
            console.log(`       Steps: ${m.integrationSteps.length}`);
            console.log(`       Snippets: ${Object.keys(m.codeSnippets || {}).length}`);
        });
        console.log('');

        console.log('='.repeat(60));
        console.log('✅ COMPLETE! Documentation generation working!');
        console.log('='.repeat(60));

    } catch (error) {
        if (error.response?.status === 400 && error.response?.data?.message?.includes('already assigned')) {
            console.log('✅ Module already assigned - testing documentation...\n');

            //Test doc generation anyway
            const headers = {
                'Authorization': `Bearer ${error.config.headers.Authorization}`
            };

            const docResponse = await axios.get(
                `${baseUrl}/applications/${appId}/documentation`,
                { headers }
            );

            const doc = docResponse.data;
            console.log(`✅ Documentation generated!`);
            console.log(`   Application: ${doc.applicationName}`);
            console.log(`   Modules: ${doc.modules.length}`);

            console.log('\n' + '='.repeat(60));
            console.log('✅ Documentation generation working!');
            console.log('='.repeat(60));
        } else {
            console.error('❌ Error:', error.response?.data || error.message);
            throw error;
        }
    }
}

assignModule()
    .then(() => process.exit(0))
    .catch(() => process.exit(1));
