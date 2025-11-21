// 📁 c:\Users\aakib\Primus SaaS\scripts\assign-fresh-version-with-delay.js
const axios = require('axios');

const baseUrl = 'http://localhost:5267/api';
const appId = 9;

async function login() {
    const { data } = await axios.post(`${baseUrl}/auth/login`, {
        email: 'admin@primussaas.com',
        password: 'Admin123!'
    });
    return data.token;
}

// simple sleep helper
function sleep(ms) { return new Promise(r => setTimeout(r, ms)); }

(async () => {
    try {
        const token = await login();
        const headers = { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' };

        // Find module
        const { data: modules } = await axios.get(`${baseUrl}/modules`, { headers });
        const module = modules.find(m => m.moduleKey === 'identity-validator' || m.name === 'IdentityValidator');
        if (!module) throw new Error('IdentityValidator module not found');
        console.log(`✅ Module ID ${module.id}`);

        // Ensure version 1.0.1 exists (create if missing)
        let { data: versions } = await axios.get(`${baseUrl}/modules/${module.id}/versions`, { headers });
        let version = versions.find(v => v.version === '1.0.1');
        if (!version) {
            console.log('🚀 Creating version 1.0.1...');
            const payload = {
                version: '1.0.1',
                isBreakingChange: false,
                releaseNotes: 'Fresh version for demo – ensures proper linking',
                changelog: 'Initial demo release',
                demoCode: `// Minimal demo code\nconst express = require('express');\nconst { primusIdentityMiddleware } = require('primus-identity-validator');\nconst app = express();\nconst primusAuth = primusIdentityMiddleware({\n  portalUrl: 'YOUR_PORTAL_URL',\n  clientId: 'YOUR_CLIENT_ID',\n  clientSecret: 'YOUR_CLIENT_SECRET',\n  jwtSecret: 'YOUR_CLIENT_SECRET',\n  mode: 'Local'\n});\napp.get('/api/protected', primusAuth, (req, res) => res.json({ user: req.primusUser }));\napp.listen(3000);`,
                supportedStacks: ['NodeJS']
            };
            await axios.post(`${baseUrl}/modules/${module.id}/versions`, payload, { headers });
            // wait a moment for DB commit
            await sleep(2000);
            ({ data: versions } = await axios.get(`${baseUrl}/modules/${module.id}/versions`, { headers }));
            version = versions.find(v => v.version === '1.0.1');
        }

        if (!version) throw new Error('Version 1.0.1 could not be found even after creation.');

        console.log(`✅ Using version ID ${version.id}`);
        console.log('🔎 Version details from list:', JSON.stringify(version, null, 2));

        // Assign
        console.log(`🔗 Assigning version ${version.id} to Application ${appId}...`);
        try {
            await axios.post(`${baseUrl}/applications/${appId}/modules`, {
                moduleId: module.id,
                moduleVersionId: version.id // NOTE: The DTO expects ModuleVersionId, not VersionId? Let's check the DTO again.
            }, { headers });
            console.log('✅ Assignment successful');
        } catch (e) {
            // Check if it failed because of property name mismatch
            if (e.response?.status === 400) {
                console.log('⚠️ Assignment failed with 400. Retrying with different property name...');
                try {
                    await axios.post(`${baseUrl}/applications/${appId}/modules`, {
                        moduleId: module.id,
                        versionId: version.id
                    }, { headers });
                    console.log('✅ Assignment successful (retry)');
                } catch (e2) {
                    if (e2.response?.data?.message?.includes('already integrated')) {
                        console.log('⚠️ Already assigned – skipping');
                    } else {
                        console.error('❌ Assignment retry failed:', e2.response?.data || e2.message);
                        throw e2;
                    }
                }
            } else {
                console.error('❌ Assignment failed:', e.response?.data || e.message);
                throw e;
            }
        }

        // Verify application now has the module
        const { data: app } = await axios.get(`${baseUrl}/applications/${appId}`, { headers });
        console.log('✅ Application after assignment:');
        console.log(JSON.stringify(app, null, 2));

        // Documentation payload
        console.log('📄 Requesting documentation...');
        const { data: doc } = await axios.get(`${baseUrl}/documentation/${appId}`, { headers });
        console.log('✅ Documentation payload:');
        console.log(JSON.stringify(doc, null, 2));

        console.log('🎉 All steps completed successfully');
    } catch (err) {
        console.error('❌ Fatal error:', err.response?.data || err.message);
        process.exit(1);
    }
})();
