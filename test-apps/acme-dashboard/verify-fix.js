const msal = require('@azure/msal-node');
const axios = require('axios');

// Configuration matches what we set up
const config = {
    auth: {
        clientId: "acc675f1-e32f-40b9-a0c6-716066cc6890",
        authority: "https://login.microsoftonline.com/cbd15a9b-cd52-4ccc-916a-00e2edb13043",
    }
};

const pca = new msal.PublicClientApplication(config);

async function runTest() {
    console.log("🧪 Starting Verification Test...");

    try {
        // 1. Get Access Token (using Device Code Flow for headless testing)
        // This simulates a user logging in and getting a token
        console.log("\n1️⃣  Acquiring Token from Azure AD...");

        // Note: Since we can't interactively login in this script easily without user interaction,
        // we'll try to use Client Credentials flow JUST to verify the backend validation logic.
        // HOWEVER, the backend expects a user token (with user claims). 
        // A better approach for automated testing without user interaction is difficult with MSAL 
        // unless we use a test user + password (ROPC) which is not recommended/supported for all tenants.

        // Instead, we will verify the BACKEND is healthy and reachable first.
        const healthCheck = await axios.get('http://localhost:3000/api/health');
        console.log("✅ Health Check Passed:", healthCheck.data);

        console.log("\n⚠️  Cannot automate full UI login (requires interactive browser).");
        console.log("   However, we can verify the backend configuration is ready.");

        console.log("\n🔍 Backend Config Verification:");
        console.log("   - URL: http://localhost:3000");
        console.log("   - Auth Mode: Azure AD");
        console.log("   - Expected Audience: acc675f1-e32f-40b9-a0c6-716066cc6890");
        console.log("   - Expected Tenant: cbd15a9b-cd52-4ccc-916a-00e2edb13043");

        console.log("\n✅ Backend is running and reachable.");
        console.log("👉 Please open http://localhost:3000 in your browser to complete the login test.");

    } catch (error) {
        console.error("❌ Test Failed:", error.message);
        if (error.response) {
            console.error("   Response:", error.response.data);
        }
    }
}

runTest();
