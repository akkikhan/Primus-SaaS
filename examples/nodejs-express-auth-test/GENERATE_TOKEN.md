# Generate Test JWT Token for Local Testing

This script generates a test JWT token that can be used with the Primus Auth Test Application in Local validation mode.

## Using Node.js (recommended)

```bash
node generate-test-token.js
```

## Using PowerShell

```powershell
.\generate-test-token.ps1
```

## Manual Generation

You can also use online tools like https://jwt.io to generate a token with these settings:

### Header
```json
{
  "alg": "HS256",
  "typ": "JWT"
}
```

### Payload
```json
{
  "sub": "test-user-123",
  "userId": "test-user-123",
  "email": "testuser@example.com",
  "name": "Test User",
  "roles": ["User", "Manager"],
  "aud": "test-client-123",
  "iss": "https://portal.primus-saas.com",
  "exp": 1735689600,
  "iat": 1700000000
}
```

### Secret
Use the value from your `.env` file:
```
test-secret-key-min-32-characters-long-for-hmac-validation
```

## Testing the Token

Once you have a token, test it:

```bash
# Set your token
$TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."

# Test user endpoint
curl -H "Authorization: Bearer $TOKEN" http://localhost:3000/api/user/profile

# Test manager endpoint
curl -H "Authorization: Bearer $TOKEN" http://localhost:3000/api/manager/reports

# Test admin endpoint (will fail with 403 if user doesn't have Admin role)
curl -H "Authorization: Bearer $TOKEN" http://localhost:3000/api/admin/settings
```
