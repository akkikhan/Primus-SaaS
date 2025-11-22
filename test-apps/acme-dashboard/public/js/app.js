// Main Application Logic
// Handles login, logout, and API calls with Azure AD tokens

// UI Elements
const loadingScreen = document.getElementById('loadingScreen');
const loginScreen = document.getElementById('loginScreen');
const dashboardScreen = document.getElementById('dashboardScreen');
const loginButton = document.getElementById('loginButton');
const logoutButton = document.getElementById('logoutButton');
const errorMessage = document.getElementById('errorMessage');

// User Info Elements
const userName = document.getElementById('userName');
const userEmail = document.getElementById('userEmail');

// Data Elements
const revenueEl = document.getElementById('revenue');
const growthEl = document.getElementById('growth');
const activeUsersEl = document.getElementById('activeUsers');
const lastUpdatedEl = document.getElementById('lastUpdated');

// Current user account
let currentAccount = null;

// Initialize app
async function initializeApp() {
    console.log("🚀 Initializing Acme Dashboard...");

    // Check if user is already logged in
    const accounts = msalInstance.getAllAccounts();

    if (accounts.length > 0) {
        console.log("✅ User already logged in");
        currentAccount = accounts[0];
        await showDashboard();
    } else {
        console.log("ℹ️ No user logged in");
        showLoginScreen();
    }
}

// Show login screen
function showLoginScreen() {
    loadingScreen.classList.add('hidden');
    loginScreen.classList.remove('hidden');
    dashboardScreen.classList.add('hidden');
}

// Show dashboard
async function showDashboard() {
    loadingScreen.classList.remove('hidden');
    loginScreen.classList.add('hidden');
    dashboardScreen.classList.add('hidden');

    try {
        // Update user info
        userName.textContent = currentAccount.name || currentAccount.username;
        userEmail.textContent = currentAccount.username;

        // Fetch dashboard data
        await fetchDashboardData();

        // Show dashboard
        loadingScreen.classList.add('hidden');
        dashboardScreen.classList.remove('hidden');

        console.log("✅ Dashboard loaded successfully");
    } catch (error) {
        console.error("❌ Error loading dashboard:", error);
        showError("Failed to load dashboard: " + error.message);
        showLoginScreen();
    }
}

// Login with Microsoft
loginButton.addEventListener('click', async () => {
    console.log("🔐 Initiating Microsoft login...");

    try {
        // Use popup for login (alternative: redirect)
        const response = await msalInstance.loginPopup(loginRequest);
        console.log("✅ Login successful");
        currentAccount = response.account;
        await showDashboard();
    } catch (error) {
        console.error("❌ Login failed:", error);
        showError("Login failed: " + error.message);
    }
});

// Logout
logoutButton.addEventListener('click', async () => {
    console.log("👋 Logging out...");

    try {
        await msalInstance.logoutPopup({
            account: currentAccount
        });
        currentAccount = null;
        showLoginScreen();
        console.log("✅ Logout successful");
    } catch (error) {
        console.error("❌ Logout failed:", error);
        showError("Logout failed: " + error.message);
    }
});

// Get access token for API calls
async function getAccessToken() {
    if (!currentAccount) {
        throw new Error("No user logged in");
    }

    try {
        // Try to get token silently (from cache)
        const response = await msalInstance.acquireTokenSilent({
            ...tokenRequest,
            account: currentAccount
        });

        console.log("✅ Access token acquired (silent)");
        return response.accessToken;
    } catch (error) {
        console.warn("⚠️ Silent token acquisition failed, trying popup...");

        // If silent fails, use popup
        try {
            const response = await msalInstance.acquireTokenPopup(tokenRequest);
            console.log("✅ Access token acquired (popup)");
            return response.accessToken;
        } catch (popupError) {
            console.error("❌ Token acquisition failed:", popupError);
            throw new Error("Failed to get access token");
        }
    }
}

// Fetch dashboard data from API
async function fetchDashboardData() {
    console.log("📊 Fetching dashboard data...");

    try {
        // Get access token
        const accessToken = await getAccessToken();
        console.log("🔑 Access token obtained");

        // Call protected API endpoint
        const response = await fetch('/api/revenue-stats', {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${accessToken}`,
                'Content-Type': 'application/json'
            }
        });

        if (!response.ok) {
            const errorData = await response.json().catch(() => ({}));
            throw new Error(errorData.message || `API returned ${response.status}`);
        }

        const data = await response.json();
        console.log("✅ Dashboard data received:", data);

        // Update UI with data
        updateDashboard(data);

    } catch (error) {
        console.error("❌ Failed to fetch dashboard data:", error);
        throw error;
    }
}

// Update dashboard with data
function updateDashboard(data) {
    revenueEl.textContent = data.revenue || "$0";
    growthEl.textContent = data.growth || "+0%";
    activeUsersEl.textContent = (data.activeUsers || 0).toLocaleString();

    if (data.lastUpdated) {
        const date = new Date(data.lastUpdated);
        lastUpdatedEl.textContent = date.toLocaleString();
    }
}

// Show error message
function showError(message) {
    errorMessage.textContent = message;
    errorMessage.classList.remove('hidden');

    setTimeout(() => {
        errorMessage.classList.add('hidden');
    }, 5000);
}

// Initialize app when page loads
window.addEventListener('DOMContentLoaded', () => {
    console.log("📄 DOM loaded, initializing app...");
    initializeApp();
});

// Handle MSAL errors globally
window.addEventListener('unhandledrejection', (event) => {
    if (event.reason && event.reason.errorCode) {
        console.error("❌ MSAL Error:", event.reason);
        showError("Authentication error: " + event.reason.errorMessage);
    }
});
