# Primus Auth Test Application

A sample Express.js application demonstrating authentication integration with **Primus SaaS Identity Validator**. This application showcases both **Local JWT validation** and **Azure AD token validation** modes.

## 🎯 Purpose

This test application allows you to:
- ✅ Verify the `primus-identity-validator` package works correctly
- ✅ Test Local JWT validation (HMAC/HS256)
- ✅ Test Azure AD token validation (RS256)
- ✅ Test Hybrid mode (Azure AD with Local fallback)
- ✅ Understand role-based access control (RBAC)
- ✅ See a complete integration example

## 📦 Installation

```bash
# Install dependencies
npm install

# Copy environment configuration
cp .env.example .env

# Edit .env with your Primus Portal credentials
# PRIMUS_CLIENT_ID, PRIMUS_CLIENT_SECRET, etc.
```

## 🔧 Configuration

Edit `.env` file with your settings:

```bash
# Required for all modes
PRIMUS_PORTAL_URL=https://portal.primus-saas.com
PRIMUS_CLIENT_ID=your-client-id
PRIMUS_CLIENT_SECRET=your-client-secret

# Choose validation mode: Local, AzureAd, or Hybrid
VALIDATION_MODE=Local

# Required if using AzureAd or Hybrid mode
AZURE_AD_TENANT_ID=your-tenant-id

# Optional
PORT=3000
JWKS_CACHE_TTL=24
```

### Validation Modes

1. **Local Mode** (HMAC/HS256)
   - Validates tokens using shared secret
   - Fast, simple, no external dependencies
   - Best for: Development, testing, simple deployments

2. **AzureAd Mode** (RS256)
   - Validates tokens using Azure AD public keys (JWKS)
   - Fetches keys from Azure AD OpenID endpoint
   - Best for: Enterprise applications, SSO integrations

3. **Hybrid Mode** (Fallback)
   - Tries Azure AD validation first
   - Falls back to Local validation if Azure AD fails
   - Best for: Migration scenarios, high availability

## 🚀 Running the Application

### Development Mode (with hot reload)
```bash
npm run dev
```

### Production Mode
```bash
npm run build
npm start
```

The server will start on `http://localhost:3000` (or your configured PORT).

## 📡 API Endpoints

### Public Endpoints (No Authentication)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | Application info and endpoint list |
| GET | `/api/public` | Public data access |
| GET | `/api/health` | Health check |

### Protected Endpoints (Authentication Required)

| Method | Endpoint | Required Role | Description |
|--------|----------|---------------|-------------|
| GET | `/api/user/profile` | Any authenticated | User profile |
| GET | `/api/user/permissions` | Any authenticated | User permissions |
| GET | `/api/admin/settings` | Admin | Admin settings |
| GET | `/api/admin/users` | Admin | User management |
| GET | `/api/manager/reports` | Manager or Admin | Reports access |
| GET | `/api/manager/team` | Manager or Admin | Team management |

## 🧪 Testing the Application

### 1. Test Public Endpoint (No Auth)

```bash
curl http://localhost:3000/api/public
```

Expected response:
```json
{
  "message": "This is a public endpoint - no authentication required",
  "timestamp": "2024-11-18T17:00:00.000Z"
}
```

### 2. Test Protected Endpoint (With Auth)

First, obtain a JWT token from your Primus Portal or generate a test token.

```bash
# Set your token
TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."

# Test user profile endpoint
curl -H "Authorization: Bearer $TOKEN" http://localhost:3000/api/user/profile
```

Expected response (if valid token):
```json
{
  "message": "User profile retrieved successfully",
  "user": {
    "userId": "12345",
    "email": "user@example.com",
    "name": "John Doe",
    "roles": ["User"]
  }
}
```

### 3. Test Role-Based Access

```bash
# Test Admin endpoint (requires Admin role)
curl -H "Authorization: Bearer $ADMIN_TOKEN" http://localhost:3000/api/admin/settings

# Test Manager endpoint (requires Manager or Admin role)
curl -H "Authorization: Bearer $MANAGER_TOKEN" http://localhost:3000/api/manager/reports
```

### 4. Test Azure AD Mode

If using Azure AD validation mode:

```bash
# Get Azure AD token using Azure CLI
az account get-access-token --resource "api://your-client-id" --query accessToken -o tsv

# Or use the token in your request
curl -H "Authorization: Bearer $AZURE_AD_TOKEN" http://localhost:3000/api/user/profile
```

## 🔍 Expected Behaviors

### Valid Token
- Status: `200 OK`
- Returns requested data
- User information available in response

### Invalid Token
- Status: `401 Unauthorized`
- Error message: "Invalid token" or "Token expired"

### Missing Token
- Status: `401 Unauthorized`
- Error message: "No authorization header provided"

### Insufficient Permissions
- Status: `403 Forbidden`
- Error message: "Insufficient permissions. Required roles: Admin"

## 🐛 Troubleshooting

### Issue: "Invalid token signature"
**Solution**: Verify `PRIMUS_CLIENT_SECRET` matches your Portal application secret.

### Issue: "Token expired"
**Solution**: Request a new token from the Portal or increase `clockSkew` tolerance.

### Issue: "No matching key found for kid"
**Solution**: In Azure AD mode, wait for JWKS cache to refresh (24 hours) or restart the app.

### Issue: "Token tenant ID does not match"
**Solution**: Ensure `AZURE_AD_TENANT_ID` matches the tenant in your Azure AD token.

### Issue: "403 Forbidden"
**Solution**: Verify your user has the required role for the endpoint (e.g., Admin, Manager).

## 📝 Code Structure

```
src/
└── index.ts          # Main application file
    ├── Configuration (dotenv, CORS)
    ├── Middleware (Primus auth)
    ├── Public routes
    ├── Protected routes
    ├── Admin routes
    ├── Manager routes
    └── Error handling
```

## 🔐 Security Notes

1. **Never commit `.env` file** - Contains secrets
2. **Use HTTPS in production** - Protect tokens in transit
3. **Rotate secrets regularly** - Update CLIENT_SECRET periodically
4. **Validate input** - Always validate user input in real applications
5. **Rate limiting** - Add rate limiting middleware for production

## 📚 Additional Resources

- [Primus Identity Validator Documentation](../../sdk/nodejs/primus-identity-validator/README.md)
- [Express.js Documentation](https://expressjs.com/)
- [JWT Best Practices](https://tools.ietf.org/html/rfc8725)
- [Azure AD Documentation](https://docs.microsoft.com/azure/active-directory/)

## 🤝 Integration Checklist

Use this checklist when integrating Primus authentication into your own application:

- [ ] Install `@primus-saas/identity-validator` package
- [ ] Configure environment variables (Portal URL, Client ID, Secret)
- [ ] Choose validation mode (Local, AzureAd, or Hybrid)
- [ ] Apply `primusIdentityMiddleware` to protected routes
- [ ] Use `requireRoles()` for role-based access control
- [ ] Access user info via `req.primusUser`
- [ ] Handle 401 and 403 errors appropriately
- [ ] Test all authentication scenarios
- [ ] Add rate limiting and security headers
- [ ] Configure CORS for your frontend domain

## 📄 License

MIT

## 💬 Support

For issues or questions:
- Check the main SDK documentation
- Review error messages in console
- Verify environment configuration
- Test with public endpoints first

---

**Version**: 1.0.0  
**Last Updated**: November 18, 2024
