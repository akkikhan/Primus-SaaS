# Token Generation Guide - Node.js SDK

This guide shows you how to generate JWT tokens that the **primus-identity-validator** package will successfully validate.

> [!IMPORTANT]
> **Critical Requirement**: The `secret`, `issuer`, and `audiences` values used when generating tokens **MUST EXACTLY MATCH** your validator configuration. Mismatches will cause validation failures.

## Table of Contents

1. [Local JWT Token Generation](#local-jwt-token-generation)
2. [Azure AD Token Acquisition](#azure-ad-token-acquisition)
3. [Critical Configuration Matching](#critical-configuration-matching)
4. [Frontend Integration Examples](#frontend-integration-examples)
5. [Common Mistakes](#common-mistakes)
6. [Testing Your Tokens](#testing-your-tokens)

---

## Local JWT Token Generation

### Prerequisites

Install the JWT library:

```bash
npm install jsonwebtoken
```

### Complete Working Example

```javascript
const jwt = require('jsonwebtoken');

function generateLocalJwtToken(userId, email, name) {
  // ⚠️ CRITICAL: These values MUST match your validator configuration
  const secret = 'your-super-secret-key-at-least-32-characters-long!';
  const issuer = 'https://localhost:4000';
  const audience = 'api://your-app-id';
  
  const payload = {
    sub: userId,
    email: email,
    name: name,
    aud: audience,
    iss: issuer,
    // Optional: Add roles for authorization
    roles: ['User', 'Admin']
  };
  
  const options = {
    expiresIn: '1h',
    issuer: issuer,
    audience: audience,
    algorithm: 'HS256'
  };
  
  return jwt.sign(payload, secret, options);
}

// Usage
const token = generateLocalJwtToken(
  'user123',
  'user@example.com',
  'John Doe'
);
console.log('Generated Token:', token);
```

### Using in an Express Auth Route

```javascript
const express = require('express');
const jwt = require('jsonwebtoken');
const router = express.Router();

// Load configuration (ensure it matches validator config)
const JWT_SECRET = process.env.JWT_SECRET || 'your-super-secret-key-at-least-32-characters-long!';
const JWT_ISSUER = process.env.JWT_ISSUER || 'https://localhost:4000';
const JWT_AUDIENCE = process.env.JWT_AUDIENCE || 'api://your-app-id';

router.post('/local-login', async (req, res) => {
  const { email, password } = req.body;
  
  // TODO: Validate credentials against your user database
  const user = await validateCredentials(email, password);
  if (!user) {
    return res.status(401).json({ error: 'Invalid credentials' });
  }
  
  // Generate token with matching configuration
  const payload = {
    sub: user.id,
    email: user.email,
    name: user.name,
    aud: JWT_AUDIENCE,
    iss: JWT_ISSUER,
    roles: user.roles || []
  };
  
  const options = {
    expiresIn: '1h',
    issuer: JWT_ISSUER,
    audience: JWT_AUDIENCE,
    algorithm: 'HS256'
  };
  
  const token = jwt.sign(payload, JWT_SECRET, options);
  
  res.json({ token });
});

async function validateCredentials(email, password) {
  // Implement your credential validation logic
  // Return user object or null
  return { id: '123', email, name: 'John Doe', roles: ['User'] };
}

module.exports = router;
```

### TypeScript Version

```typescript
import jwt from 'jsonwebtoken';

interface TokenPayload {
  sub: string;
  email: string;
  name: string;
  aud: string;
  iss: string;
  roles?: string[];
}

export function generateLocalJwtToken(
  userId: string,
  email: string,
  name: string,
  roles: string[] = []
): string {
  const secret = process.env.JWT_SECRET || 'your-super-secret-key-at-least-32-characters-long!';
  const issuer = process.env.JWT_ISSUER || 'https://localhost:4000';
  const audience = process.env.JWT_AUDIENCE || 'api://your-app-id';
  
  const payload: TokenPayload = {
    sub: userId,
    email,
    name,
    aud: audience,
    iss: issuer,
    roles
  };
  
  const options: jwt.SignOptions = {
    expiresIn: '1h',
    issuer,
    audience,
    algorithm: 'HS256'
  };
  
  return jwt.sign(payload, secret, options);
}
```

---

## Azure AD Token Acquisition

For Azure AD OIDC tokens, you don't generate them yourself - users authenticate with Microsoft and receive tokens.

### Frontend (MSAL Browser)

```javascript
import { PublicClientApplication } from '@azure/msal-browser';

const msalConfig = {
  auth: {
    clientId: 'YOUR_CLIENT_ID',
    authority: 'https://login.microsoftonline.com/YOUR_TENANT_ID',
    redirectUri: 'http://localhost:3000'
  }
};

const msalInstance = new PublicClientApplication(msalConfig);

async function acquireAzureAdToken() {
  const loginRequest = {
    scopes: ['api://YOUR_API_ID/.default']
  };
  
  try {
    // Try silent acquisition first
    const accounts = msalInstance.getAllAccounts();
    if (accounts.length > 0) {
      const silentRequest = {
        ...loginRequest,
        account: accounts[0]
      };
      const response = await msalInstance.acquireTokenSilent(silentRequest);
      return response.accessToken;
    }
    
    // Interactive login required
    const response = await msalInstance.loginPopup(loginRequest);
    return response.accessToken;
  } catch (error) {
    console.error('Token acquisition failed:', error);
    throw error;
  }
}
```

### Backend (Confidential Client)

```javascript
const msal = require('@azure/msal-node');

const msalConfig = {
  auth: {
    clientId: process.env.AZURE_CLIENT_ID,
    authority: `https://login.microsoftonline.com/${process.env.AZURE_TENANT_ID}`,
    clientSecret: process.env.AZURE_CLIENT_SECRET
  }
};

const cca = new msal.ConfidentialClientApplication(msalConfig);

async function acquireTokenForApi() {
  const tokenRequest = {
    scopes: ['api://YOUR_API_ID/.default']
  };
  
  try {
    const response = await cca.acquireTokenByClientCredential(tokenRequest);
    return response.accessToken;
  } catch (error) {
    console.error('Token acquisition failed:', error);
    throw error;
  }
}
```

---

## Critical Configuration Matching

> [!CAUTION]
> **Token validation will fail if these values don't match exactly between generation and validation.**

### Your Validator Configuration

```javascript
const primusAuth = primusIdentityMiddleware({
  issuers: [
    {
      name: 'LocalAuth',
      type: 'jwt',
      issuer: 'https://localhost:4000',
      secret: 'your-super-secret-key-at-least-32-characters-long!',
      audiences: ['api://your-app-id']
    }
  ]
});
```

### Your Token Generation Code MUST Use

| Configuration Value | Token Generation Value | Where Used |
|---------------------|------------------------|------------|
| `issuers[0].secret` | `jwt.sign(payload, secret, ...)` | Signature generation |
| `issuers[0].issuer` | `options.issuer` and `payload.iss` | Issuer claim |
| `issuers[0].audiences[0]` | `options.audience` and `payload.aud` | Audience claim |

### Best Practice: Use Environment Variables

```javascript
// .env file
JWT_SECRET=your-super-secret-key-at-least-32-characters-long!
JWT_ISSUER=https://localhost:4000
JWT_AUDIENCE=api://your-app-id

// Token generation
const secret = process.env.JWT_SECRET;
const issuer = process.env.JWT_ISSUER;
const audience = process.env.JWT_AUDIENCE;

// Validator configuration
const primusAuth = primusIdentityMiddleware({
  issuers: [
    {
      name: 'LocalAuth',
      type: 'jwt',
      issuer: process.env.JWT_ISSUER,  // ✅ Same source
      secret: process.env.JWT_SECRET,  // ✅ Same source
      audiences: [process.env.JWT_AUDIENCE]  // ✅ Same source
    }
  ]
});
```

---

## Frontend Integration Examples

### React with Fetch API

```jsx
import React, { useState } from 'react';

function LoginComponent() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [token, setToken] = useState(null);
  
  const handleLogin = async (e) => {
    e.preventDefault();
    
    try {
      // Get token from your auth endpoint
      const response = await fetch('http://localhost:4000/api/auth/local-login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password })
      });
      
      const data = await response.json();
      
      if (response.ok) {
        setToken(data.token);
        sessionStorage.setItem('authToken', data.token);
        console.log('Login successful!');
      } else {
        console.error('Login failed:', data.error);
      }
    } catch (error) {
      console.error('Login error:', error);
    }
  };
  
  const callProtectedApi = async () => {
    try {
      const response = await fetch('http://localhost:4000/api/protected', {
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });
      
      const data = await response.json();
      console.log('Protected data:', data);
    } catch (error) {
      console.error('API call failed:', error);
    }
  };
  
  return (
    <div>
      <form onSubmit={handleLogin}>
        <input
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="Email"
        />
        <input
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          placeholder="Password"
        />
        <button type="submit">Login</button>
      </form>
      
      {token && (
        <button onClick={callProtectedApi}>Call Protected API</button>
      )}
    </div>
  );
}
```

### Axios Interceptor

```javascript
import axios from 'axios';

// Create axios instance
const api = axios.create({
  baseURL: 'http://localhost:4000/api'
});

// Add token to all requests
api.interceptors.request.use(
  (config) => {
    const token = sessionStorage.getItem('authToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Usage
async function fetchProtectedData() {
  try {
    const response = await api.get('/protected');
    console.log('Data:', response.data);
  } catch (error) {
    console.error('Error:', error);
  }
}
```

### Vanilla JavaScript

```javascript
// Login and store token
async function login(email, password) {
  const response = await fetch('http://localhost:4000/api/auth/local-login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password })
  });
  
  const data = await response.json();
  
  if (response.ok) {
    sessionStorage.setItem('authToken', data.token);
    return data.token;
  } else {
    throw new Error(data.error || 'Login failed');
  }
}

// Call protected endpoint
async function callProtectedEndpoint() {
  const token = sessionStorage.getItem('authToken');
  
  const response = await fetch('http://localhost:4000/api/protected', {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  
  return await response.json();
}
```

---

## Common Mistakes

### ❌ Mistake #1: Secret Key Mismatch

```javascript
// Token Generation
const secret = 'secret-key-123';

// Validator Configuration
secret: 'different-secret-key-456'  // ❌ Won't validate!
```

**Error**: `Invalid signature`  
**Fix**: Use the exact same secret in both places.

---

### ❌ Mistake #2: Issuer Format Incorrect

```javascript
// Token Generation
issuer: 'LocalAuth'  // ❌ Wrong! This is a name, not a URL

// Validator Configuration
issuer: 'https://localhost:4000'  // Expects full URL
```

**Error**: `Untrusted issuer: LocalAuth`  
**Fix**: Use full URL format: `https://localhost:4000`

---

### ❌ Mistake #3: Audience Mismatch

```javascript
// Token Generation
audience: 'http://localhost:4000'  // ❌ Wrong format

// Validator Configuration
audiences: ['api://your-app-id']
```

**Error**: `Invalid audience`  
**Fix**: Use the API identifier format: `api://your-app-id`

---

### ❌ Mistake #4: Missing Required Claims

```javascript
// ❌ Missing 'sub' claim
const payload = {
  email: 'user@example.com',
  name: 'John Doe'
  // Missing: sub: userId
};
```

**Error**: `req.primusUser` is incomplete or null  
**Fix**: Always include `sub`, `email`, and `name` claims.

---

### ❌ Mistake #5: Wrong Algorithm

```javascript
// ❌ Using RS256 for Local JWT
const options = {
  algorithm: 'RS256'  // Wrong for shared secret!
};

// ✅ Correct for Local JWT
const options = {
  algorithm: 'HS256'  // HMAC with shared secret
};
```

---

## Testing Your Tokens

### 1. Decode Token at jwt.io

Visit [https://jwt.io](https://jwt.io) and paste your token to verify:

- **Header**: Should show `"alg": "HS256"` for Local JWT
- **Payload**: Check `iss`, `aud`, `sub`, `exp` claims
- **Signature**: Paste your secret to verify signature is valid

### 2. Test Against Your API

```javascript
const fetch = require('node-fetch');

async function testToken() {
  const token = generateLocalJwtToken('user123', 'user@example.com', 'John Doe');
  
  const response = await fetch('http://localhost:4000/api/protected', {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  
  if (response.ok) {
    console.log('✅ Token validated successfully!');
    const data = await response.json();
    console.log(data);
  } else {
    console.log(`❌ Validation failed: ${response.status}`);
    const error = await response.text();
    console.log(error);
  }
}

testToken();
```

### 3. Verify Token Manually

```javascript
const jwt = require('jsonwebtoken');

function verifyToken(token, secret) {
  try {
    const decoded = jwt.verify(token, secret);
    console.log('✅ Token is valid');
    console.log('Decoded payload:', decoded);
    return decoded;
  } catch (error) {
    console.log('❌ Token verification failed:', error.message);
    return null;
  }
}

// Test
const token = generateLocalJwtToken('user123', 'user@example.com', 'John Doe');
verifyToken(token, 'your-super-secret-key-at-least-32-characters-long!');
```

---

## Quick Reference Checklist

Before deploying your token generation code:

- [ ] Secret key matches between generation and validation
- [ ] Issuer is full URL format (e.g., `https://localhost:4000`)
- [ ] Audience uses API identifier format (e.g., `api://your-app-id`)
- [ ] All required claims included: `sub`, `email`, `name`, `aud`, `iss`
- [ ] Token expiration set appropriately (`expiresIn`)
- [ ] Algorithm is `HS256` for Local JWT
- [ ] Configuration values loaded from environment variables
- [ ] Tested token at jwt.io to verify structure
- [ ] Tested against actual API endpoint
- [ ] Frontend properly sends token in `Authorization: Bearer <token>` header

---

## Next Steps

- See [ERROR_REFERENCE.md](./ERROR_REFERENCE.md) for detailed troubleshooting
- See [PRODUCTION_DEPLOYMENT.md](./PRODUCTION_DEPLOYMENT.md) for secret management in production
- See [README.md](./README.md) for validator configuration examples

---

**Need Help?**
- GitHub Issues: https://github.com/akkikhan/Primus-SaaS/issues
- Documentation: https://portal.primus-saas.com/docs
