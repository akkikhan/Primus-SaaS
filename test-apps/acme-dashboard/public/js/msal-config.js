// MSAL Configuration
// This file configures Microsoft Authentication Library (MSAL) for Azure AD authentication

// Azure AD Configuration
// IMPORTANT: These are real credentials for the Acme Dashboard app
const msalConfig = {
    auth: {
        clientId: "acc675f1-e32f-40b9-a0c6-716066cc6890", // Application (client) ID from Azure Portal
        authority: "https://login.microsoftonline.com/cbd15a9b-cd52-4ccc-916a-00e2edb13043", // Tenant-specific authority
        redirectUri: window.location.origin, // Current page URL
    },
    cache: {
        cacheLocation: "sessionStorage", // Store tokens in session storage
        storeAuthStateInCookie: false, // Set to true for IE11 or Edge
    }
};

// Scopes for token request
// This tells Azure AD what permissions we need
const loginRequest = {
    scopes: ["User.Read"] // Basic profile information
};

// Scopes for API access
// Request access token for THIS application (Client ID)
const tokenRequest = {
    scopes: [
        "acc675f1-e32f-40b9-a0c6-716066cc6890/.default"
    ]
};

// Initialize MSAL instance
const msalInstance = new msal.PublicClientApplication(msalConfig);

// Handle redirect promise
msalInstance.handleRedirectPromise()
    .then(response => {
        if (response) {
            console.log("✅ Login successful via redirect");
            handleLoginSuccess(response.account);
        }
    })
    .catch(error => {
        console.error("❌ Redirect error:", error);
    });

// Export for use in app.js
window.msalInstance = msalInstance;
window.loginRequest = loginRequest;
window.tokenRequest = tokenRequest;
