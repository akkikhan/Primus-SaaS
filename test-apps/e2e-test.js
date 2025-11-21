const axios = require('axios');

const BASE_URL = 'http://localhost:5267/api';

async function runE2ETest() {
    console.log('='.repeat(60));
    console.log('🧪 PRIMUS SAAS - END-TO-END INTEGRATION TEST');
    console.log('='.repeat(60));
    console.log('');

    try {
        // Step 1: Login
        console.log('Step 1: 🔐 Logging in...');
        const loginResponse = await axios.post(`${BASE_URL}/auth/login`, {
            email: 'admin@primussaas.com',
            password: 'Admin123!'
        });

        const { token, email, role } = loginResponse.data;
        console.log(`✅ Login successful!`);
        console.log(`   Email: ${email}`);
        console.log(`   Role: ${role}`);
        console.log(`   Token: ${token.substring(0, 20)}...`);
        console.log('');

        const headers = {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
        };

        // Step 2: Create a new application
        console.log('Step 2: 📱 Creating new application...');
        const appResponse = await axios.post(`${BASE_URL}/applications`, {
            name: 'E2E Test Demo App',
            stack: 'NodeJS',
            description: 'End-to-end test application created via automation'
        }, { headers });

        const newApp = appResponse.data;
        console.log(`✅ Application created successfully!`);
        console.log(`   App ID: ${newApp.id}`);
        console.log(`   Name: ${newApp.name}`);
        console.log(`   Stack: ${newApp.stack}`);
        console.log(`   Client ID: ${newApp.primusClientId}`);
        console.log(`   Client Secret: ${newApp.clientSecret}`);
        console.log('');

        // Step 3: Get application details
        console.log('Step 3: 🔍 Fetching application details...');
        const appDetailsResponse = await axios.get(
            `${BASE_URL}/applications/${newApp.id}`,
            { headers }
        );

        const appDetails = appDetailsResponse.data;
        console.log(`✅ Application details retrieved!`);
        console.log(`   ID: ${appDetails.id}`);
        console.log(`   Name: ${appDetails.name}`);
        console.log(`   Stack: ${appDetails.stack}`);
        console.log(`   Created: ${new Date(appDetails.createdAt).toLocaleString()}`);
        console.log('');

        // Summary: Return credentials for demo integration
        console.log('='.repeat(60));
        console.log('✅ E2E TEST COMPLETED SUCCESSFULLY!');
        console.log('='.repeat(60));
        console.log('');
        console.log('📦 Application Credentials (for SDK integration):');
        console.log('-'.repeat(60));
        console.log(`Client ID:     ${newApp.primusClientId}`);
        console.log(`Client Secret: ${newApp.clientSecret}`);
        console.log(`Stack:         ${newApp.stack}`);
        console.log('');

        return {
            appId: newApp.id,
            clientId: newApp.primusClientId,
            clientSecret: newApp.clientSecret,
            stack: newApp.stack,
            token: token
        };

    } catch (error) {
        console.error('❌ Test failed:', error.response?.data || error.message);
        throw error;
    }
}

// Run the test
runE2ETest()
    .then(credentials => {
        console.log('Test completed. Credentials saved for next step.');
        // Store credentials in a file for the demo integration
        const fs = require('fs');
        fs.writeFileSync('e2e-credentials.json', JSON.stringify(credentials, null, 2));
        console.log('✅ Credentials saved to e2e-credentials.json');
    })
    .catch(error => {
        console.error('Test execution failed');
        process.exit(1);
    });
