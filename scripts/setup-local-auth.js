const axios = require('axios');

const PORTAL_URL = 'http://localhost:5267/api';
const ADMIN_EMAIL = 'admin@primussaas.com';
const ADMIN_PASSWORD = 'Admin123!';

async function setupLocalAuth() {
    try {
        console.log("🚀 Setting up Local Auth Environment...");

        // 1. Login as Portal Admin
        console.log("\n1️⃣  Logging in as Admin...");
        const loginRes = await axios.post(`${PORTAL_URL}/Auth/login`, {
            email: ADMIN_EMAIL,
            password: ADMIN_PASSWORD
        });
        const adminToken = loginRes.data.token;
        console.log("   ✅ Admin Token acquired");

        // 2. Create an Application
        console.log("\n2️⃣  Creating 'Local Test App'...");
        const appRes = await axios.post(`${PORTAL_URL}/Applications`, {
            name: "Local Test App",
            stack: 2, // NodeJS
            description: "App for testing Local Mode Auth"
        }, {
            headers: { Authorization: `Bearer ${adminToken}` }
        });

        const app = appRes.data;
        console.log(`   ✅ App Created: ${app.name}`);
        console.log(`   - Client ID: ${app.primusClientId}`);
        console.log(`   - Signing Key: ${app.jwtSigningKey}`); // This is what we need!

        if (!app.jwtSigningKey) {
            console.error("   ❌ ERROR: JwtSigningKey is empty! Check backend implementation.");
            return;
        }

        // 3. Register an App User
        console.log("\n3️⃣  Registering App User 'localuser'...");
        const userRes = await axios.post(`${PORTAL_URL}/auth/app/register`, {
            clientId: app.primusClientId,
            username: "localuser",
            email: "localuser@example.com",
            password: "Password123!"
        });
        console.log(`   ✅ User Registered: ${userRes.data.username}`);

        // 4. Login as App User
        console.log("\n4️⃣  Logging in as App User...");
        const appLoginRes = await axios.post(`${PORTAL_URL}/auth/app/login`, {
            clientId: app.primusClientId,
            username: "localuser",
            password: "Password123!"
        });

        const userToken = appLoginRes.data.token;
        console.log("   ✅ App User Token acquired!");
        console.log(`   - Token: ${userToken.substring(0, 20)}...`);

        // 5. Output Configuration for SDK
        console.log("\n📝 SDK CONFIGURATION FOR LOCAL MODE:");
        console.log("==================================================");
        console.log(`const PRIMUS_CONFIG = {`);
        console.log(`    portalUrl: 'http://localhost:5267',`);
        console.log(`    clientId: '${app.primusClientId}',`);
        console.log(`    clientSecret: '${app.clientSecretHash}', // (Not used for local validation but required by type)`);
        console.log(`    mode: 'Local',`);
        console.log(`    jwtSecret: '${app.jwtSigningKey}' // <--- CRITICAL`);
        console.log(`};`);
        console.log("==================================================");

    } catch (error) {
        console.error("❌ Setup Failed:", error.message);
        if (error.response) {
            console.error("   Data:", error.response.data);
        }
    }
}

setupLocalAuth();
