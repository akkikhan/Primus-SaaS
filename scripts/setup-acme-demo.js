const axios = require('axios');
const fs = require('fs');
const path = require('path');

const API_URL = 'http://localhost:5267/api';

async function setupAcmeDemo() {
    try {
        // 1. Login as Admin
        console.log('Logging in...');
        const login = await axios.post(`${API_URL}/auth/login`, {
            email: 'admin@primussaas.com',
            password: 'Admin123!'
        });
        const token = login.data.token;
        const headers = { Authorization: `Bearer ${token}` };

        // 2. Create App
        console.log('Creating Acme App...');
        const appRes = await axios.post(`${API_URL}/applications`, {
            name: `Acme Financials ${Date.now()}`, // Unique name
            stack: 'NodeJS',
            description: 'Demo App'
        }, { headers });
        const app = appRes.data;

        // 3. Get Module ID
        const modulesRes = await axios.get(`${API_URL}/modules`, { headers });
        const module = modulesRes.data.find(m => m.moduleKey === 'identity-validator');

        if (!module) throw new Error('Identity Validator module not found');

        // 4. Get Version ID
        const versionsRes = await axios.get(`${API_URL}/modules/${module.id}/versions`, { headers });
        console.log(`Found ${versionsRes.data.length} versions.`);

        let version = versionsRes.data.find(v => {
            const stacks = JSON.parse(v.supportedStacksJson || '[]');
            return stacks.includes('NodeJS');
        });

        if (!version) {
            console.log('⚠️ No specific NodeJS version found. Using the latest available version.');
            version = versionsRes.data[0];
        }

        if (!version) {
            throw new Error('No versions found at all.');
        }
        console.log(`Selected Version: ${version.version} (ID: ${version.id})`);

        // 5. Assign Module
        console.log('Assigning Module...');
        try {
            await axios.post(`${API_URL}/applications/${app.id}/modules`, {
                moduleId: module.id,
                versionId: version.id
            }, { headers });
        } catch (assignErr) {
            console.log('⚠️ Assignment failed (ignoring):', assignErr.response?.data || assignErr.message);
        }

        // 6. Output Credentials
        const credentials = {
            clientId: app.primusClientId,
            clientSecret: app.clientSecret
        };

        console.log('SUCCESS:', JSON.stringify(credentials));

        // Save to file for easy reading
        fs.writeFileSync('acme-credentials.json', JSON.stringify(credentials));

    } catch (err) {
        console.error('ERROR:', err.response?.data || err.message);
        process.exit(1);
    }
}

setupAcmeDemo();
