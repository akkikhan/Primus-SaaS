# Token Generation Guide

This guide explains how to generate valid JWT tokens for testing your integration, specifically when using a "Local" or "Custom" JWT issuer.

## Prerequisites

Ensure you have a **JWT Issuer** configured in your application:

```json
{
  "name": "LocalAuth",
  "type": "jwt",
  "issuer": "http://localhost:4000",
  "secret": "your-256-bit-secret",
  "audiences": ["api://your-app"]
}
```

## Generating Tokens in .NET

You can use `System.IdentityModel.Tokens.Jwt` to generate tokens.

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

var secret = "your-256-bit-secret";
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

var claims = new[]
{
    new Claim("sub", "user-123"),
    new Claim("email", "user@example.com"),
    new Claim("name", "John Doe"),
    new Claim("roles", "Admin")
};

var token = new JwtSecurityToken(
    issuer: "http://localhost:4000",
    audience: "api://your-app",
    claims: claims,
    expires: DateTime.Now.AddHours(1),
    signingCredentials: creds
);

var jwt = new JwtSecurityTokenHandler().WriteToken(token);
Console.WriteLine(jwt);
```

## Generating Tokens in Node.js

You can use the `jsonwebtoken` library.

```javascript
const jwt = require('jsonwebtoken');

const payload = {
  sub: 'user-123',
  email: 'user@example.com',
  name: 'John Doe',
  roles: ['Admin']
};

const secret = 'your-256-bit-secret';
const options = {
  issuer: 'http://localhost:4000',
  audience: 'api://your-app',
  expiresIn: '1h'
};

const token = jwt.sign(payload, secret, options);
console.log(token);
```

## Using the Token

Once generated, send the token in the `Authorization` header of your HTTP requests:

```http
GET /api/protected HTTP/1.1
Host: localhost:3000
Authorization: Bearer <your_jwt_token>
```
