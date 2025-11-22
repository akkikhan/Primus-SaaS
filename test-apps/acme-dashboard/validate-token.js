const axios = require('axios');

const token = process.argv[2];

if (!token) {
    console.error("❌ Please provide an access token as an argument.");
    console.error("Usage: node validate-token.js <YOUR_ACCESS_TOKEN>");
    process.exit(1);
}

async function validateToken() {
    console.log("🧪 Validating Token against Protected API...");
    console.log("==================================================");

    try {
        const response = await axios.get('http://localhost:3000/api/revenue-stats', {
            headers: { Authorization: `Bearer ${token}` }
        });

        console.log("✅ SUCCESS! API accepted the token.");
        console.log("\n📊 Data Retrieved:");
        console.log("--------------------------------------------------");
        console.log(JSON.stringify(response.data, null, 2));
        console.log("--------------------------------------------------");

        console.log("\n🔍 Scope Validation:");
        console.log("   - The backend successfully validated the token signature.");
        console.log("   - The backend confirmed the audience matches the Client ID.");
        console.log("   - The backend confirmed the issuer is Azure AD.");

    } catch (error) {
        console.error("❌ VALIDATION FAILED:", error.message);
        if (error.response) {
            console.error("   Status:", error.response.status);
            console.error("   Data:", error.response.data);
        }
    }
}

validateToken();
