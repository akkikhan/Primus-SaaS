const axios = require('axios');

const baseUrl = 'http://localhost:5267/api';

async function fixModules() {
    try {
        // Login
        const loginResponse = await axios.post(`${baseUrl}/auth/login`, {
            email: 'admin@primussaas.com',
            password: 'Admin123!'
        });
        const { token } = loginResponse.data;
        const headers = { Authorization: `Bearer ${token}` };

        // Get Module
        const modulesResponse = await axios.get(`${baseUrl}/modules`, { headers });
        const module = modulesResponse.data.find(m => m.moduleKey === 'identity-validator');

        if (!module) throw new Error('Module not found');

        // Create New Version
        const newVersion = {
            version: '1.2.1',
            releaseNotes: 'Fix for supported stacks',
            changelog: 'Fixed metadata',
            isBreakingChange: false,
            supportedStacks: ['NodeJS'],
            integrationSteps: ['npm install primus-identity-validator'],
            demoCode: '// Code here'
        };

        console.log('Creating version 1.1.0...');
        try {
            await axios.post(`${baseUrl}/modules/${module.id}/versions`, newVersion, { headers });
            console.log('✅ Created version 1.1.0');
        } catch (e) {
            console.log('⚠️ Version 1.1.0 might already exist:', e.response?.data?.message || e.message);
        }

    } catch (error) {
        console.error('❌ Error:', error.message);
    }
}

fixModules();
