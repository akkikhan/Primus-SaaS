const express = require('express');
// 1. Import the Primus SDK
const { primusIdentityMiddleware } = require('primus-identity-validator');

const app = express();

console.log("⚠️  Demo App Starting...");

// ==========================================
// 2. PASTE CONFIGURATION HERE
// ==========================================
// (Copy this from the Portal "Overview" tab)

const primusAuth = primusIdentityMiddleware({
    portalUrl: 'http://localhost:5267',
    clientId: 'REPLACE_WITH_CLIENT_ID',     // <--- PASTE HERE
    clientSecret: 'REPLACE_WITH_SECRET',    // <--- PASTE HERE
    mode: 'Local'
});

// ==========================================


// 3. Public Route (No Auth)
app.get('/public', (req, res) => {
    res.json({
        status: 'Public',
        message: 'Anyone can see this!'
    });
});

// ==========================================
// 4. ADD PROTECTION HERE
// ==========================================

// app.get('/secure', primusAuth, (req, res) => {
//     res.json({ 
//         status: 'Secure', 
//         user: req.primusUser 
//     });
// });

// ==========================================

app.listen(3000, () => {
    console.log('🚀 Executive Demo running on http://localhost:3000');
});
