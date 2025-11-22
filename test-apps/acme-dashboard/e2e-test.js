const msal = require('@azure/msal-node');
const axios = require('axios');

// Configuration
const msalConfig = {
    auth: {
        clientId: "acc675f1-e32f-40b9-a0c6-716066cc6890",
        authority: "https://login.microsoftonline.com/cbd15a9b-cd52-4ccc-916a-00e2edb13043",
    }
};

const pca = new msal.PublicClientApplication(msalConfig);

const API_URL = 'http://localhost:3000/api';

async function runE2ETest() {
    console.log("\n🧪 STARTING END-TO-END FLOW TEST");
    console.log("==================================================");

    try {
        // ---------------------------------------------------------
        // TEST 1: Public Endpoint (Health Check)
        // ---------------------------------------------------------
        console.log("\n1️⃣  Testing Public Scope (Health Check)...");
        const health = await axios.get(`${API_URL}/health`);
        if (health.status === 200 && health.data.status === 'healthy') {
            console.log("   ✅ SUCCESS: Health endpoint is accessible.");
        } else {
            throw new Error("Health check failed");
        }

        // ---------------------------------------------------------
        // TEST 2: Protected Endpoint (Unauthorized)
        // ---------------------------------------------------------
        console.log("\n2️⃣  Testing Security Scope (Unauthorized Access)...");
        try {
            await axios.get(`${API_URL}/revenue-stats`);
            console.log("   ❌ FAILED: Protected endpoint allowed access without token!");
        } catch (err) {
            if (err.response && err.response.status === 401) {
                console.log("   ✅ SUCCESS: Protected endpoint blocked unauthorized access (401).");
            } else {
                console.log(`   ⚠️  Unexpected error: ${err.message}`);
            }
        }

        // ---------------------------------------------------------
        // TEST 3: Authenticated Flow (Device Code)
        // ---------------------------------------------------------
        console.log("\n3️⃣  Testing Authenticated Use Case...");

        const deviceCodeRequest = {
            deviceCodeCallback: (response) => {
                console.log("\n   👉 ACTION REQUIRED:");
                console.log("   --------------------------------------------------");
                console.log(`   1. Go to: ${response.verificationUri}`);
                console.log(`   2. Enter Code: ${response.userCode}`);
                console.log("   --------------------------------------------------");
                console.log("   (Waiting for you to login...)");
            },
            scopes: ["openid", "profile", "email"],
        };

        const response = await pca.acquireTokenByDeviceCode(deviceCodeRequest);
        console.log("\n   ✅ Token Acquired!");
        console.log(`   - User: ${response.account.username}`);
        console.log(`   - Scopes: ${response.scopes.join(' ')}`);

        // ---------------------------------------------------------
        // TEST 4: Protected Endpoint (Authorized)
        // ---------------------------------------------------------
        console.log("\n4️⃣  Validating Data Access (With Token)...");

        const apiResponse = await axios.get(`${API_URL}/revenue-stats`, {
            headers: { Authorization: `Bearer ${response.accessToken}` }
        });

        console.log("   ✅ API Response Received:");
        console.log("   --------------------------------------------------");
        console.log(JSON.stringify(apiResponse.data, null, 2));
        console.log("   --------------------------------------------------");

        // Validate Use Case Data
        if (apiResponse.data.company === "Acme Corp" && apiResponse.data.revenue) {
            console.log("\n✅ USE CASE VALIDATED: Financial data retrieved successfully.");
        } else {
            console.log("\n❌ DATA MISMATCH: Response did not contain expected financial data.");
        }

    } catch (error) {
        console.error("\n❌ TEST FAILED:", error.message);
        if (error.response) {
            console.error("   API Error:", error.response.data);
        }
    }
    console.log("\n==================================================");
}

runE2ETest();
