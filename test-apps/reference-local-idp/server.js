const express = require('express');
const jwt = require('jsonwebtoken');
const cors = require('cors');
const bodyParser = require('body-parser');

const app = express();
const PORT = 4000;
const SECRET_KEY = 'local-dev-secret-123'; // In production, this would be in a Key Vault

app.use(cors());
app.use(bodyParser.json());

// Mock User Database
const USERS = [
    { id: 1, username: 'localuser', password: 'password123', role: 'User', tenantId: 'tenant-local-001' },
    { id: 2, username: 'admin', password: 'adminpassword', role: 'Admin', tenantId: 'tenant-local-001' }
];

// Login Endpoint
app.post('/login', (req, res) => {
    const { username, password } = req.body;

    // 1. Validate Credentials
    const user = USERS.find(u => u.username === username && u.password === password);
    
    if (!user) {
        return res.status(401).json({ message: 'Invalid credentials' });
    }

    // 2. Issue JWT
    const token = jwt.sign(
        {
            sub: user.id,
            username: user.username,
            role: user.role,
            tid: user.tenantId, // Tenant ID claim
            iss: `http://localhost:${PORT}`, // Issuer
            aud: 'acc675f1-e32f-40b9-a0c6-716066cc6890' // Audience (matches Client ID of the app)
        },
        SECRET_KEY,
        { expiresIn: '1h' }
    );

    console.log(`✅ Issued token for ${user.username}`);
    res.json({ token });
});

// JWKS Endpoint (Optional, for asymmetric keys, but we use symmetric for this simple ref)
// In a real scenario with RS256, we would expose public keys here.

app.listen(PORT, () => {
    console.log(`🔐 Reference Local IdP running on http://localhost:${PORT}`);
    console.log(`   - Issuer: http://localhost:${PORT}`);
    console.log(`   - Secret: ${SECRET_KEY}`);
});
