const axios = require('axios');

const baseUrl = 'http://localhost:5267/api';
const appId = 9;

async function testDocumentation() {
    console.log('='.repeat(60));
    console.log('📄 TESTING DOCUMENTATION GENERATION');
    console.log('='.repeat(60));
    console.log('');

    try {
        // Login
        const loginResponse = await axios.post(`${baseUrl}/auth/login`, {
            email: 'admin@primussaas.com',
            password: 'Admin123!'
        });

        const { token } = loginResponse.data;
        const headers = { 'Authorization': `Bearer ${token}` };

        // Get application details
        console.log(`Fetching Application ${appId}...`);
        const appResponse = await axios.get(`${baseUrl}/applications/${appId}`, { headers });
        const app = appResponse.data;

        console.log(`✅ Application: ${app.name}`);
        console.log(`   Client ID: ${app.primusClientId}`);
        console.log(`   Stack: ${app.stack}\n`);

        // Generate documentation
        console.log('Generating documentation...');
        const docResponse = await axios.get(
            `${baseUrl}/applications/${appId}/documentation`,
            { headers }
        );

        const doc = docResponse.data;
        console.log(`✅ Documentation Generated!\n`);
        console.log('Documentation Structure:');
        console.log(`  Application: ${doc.applicationName}`);
        console.log(`  Client ID: ${doc.primusClientId}`);
        console.log(`  Stack: ${doc.stack}`);
        console.log(`  Generated: ${doc.generatedAt}`);
        console.log(`  Modules: ${doc.modules?.length || 0}\n`);

        if (doc.modules && doc.modules.length > 0) {
            doc.modules.forEach((m, idx) => {
                console.log(`Module ${idx + 1}: ${m.moduleName} v${m.version}`);
                console.log(`  Breaking Change: ${m.isBreakingChange ? 'Yes' : 'No'}`);
                console.log(`  Release Notes: ${m.releaseNotes?.substring(0, 50)}...`);
                console.log(`  Integration Steps: ${m.integrationSteps?.length || 0}`);
                if (m.integrationSteps) {
                    m.integrationSteps.forEach((step, i) => {
                        console.log(`    ${i + 1}. ${step.substring(0, 60)}...`);
                    });
                }
                console.log(`  Demo Code: ${m.demoCode?.length || 0} characters`);
                console.log('');
            });

            console.log('='.repeat(60));
            console.log('✅ SUCCESS! Documentation generation is working!');
            console.log('='.repeat(60));
            console.log('');
            console.log('🎉 Users can now:');
            console.log('   • View documentation in the portal');
            console.log('   • Export as PDF');
            console.log('   • Export as Markdown');
            console.log('   • Export as JSON');
            console.log('   • Get step-by-step integration guides');
            console.log('');
        } else {
            console.log('⚠️  No modules assigned to this application');
            console.log('   Run assign-module.js to assign modules');
        }

    } catch (error) {
        console.error('❌ Error:', error.response?.data || error.message);
        if (error.response) {
            console.error('Status:', error.response.status);
            console.error('Data:', error.response.data);
        }
        throw error;
    }
}

testDocumentation()
    .then(() => process.exit(0))
    .catch(() => process.exit(1));
