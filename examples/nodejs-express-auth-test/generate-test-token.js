/**
 * Generate Test JWT Token for Local Testing
 * 
 * This script generates a valid JWT token for testing the Primus Auth Test Application
 * in Local validation mode.
 */

const jwt = require('jsonwebtoken');

// Configuration from .env
const CLIENT_ID = 'test-client-123';
const CLIENT_SECRET = 'test-secret-key-min-32-characters-long-for-hmac-validation';
const PORTAL_URL = 'https://portal.primus-saas.com';

// Token payload
const payload = {
  sub: 'test-user-123',
  userId: 'test-user-123',
  email: 'testuser@example.com',
  name: 'Test User',
  role: ['User', 'Manager'], // Use 'role' (singular) as per SDK expectation. Change to ['User', 'Manager', 'Admin'] for admin access
  aud: CLIENT_ID,
  iss: PORTAL_URL,
  iat: Math.floor(Date.now() / 1000),
  exp: Math.floor(Date.now() / 1000) + (60 * 60 * 24) // 24 hours from now
};

// Generate token
const token = jwt.sign(payload, CLIENT_SECRET, { algorithm: 'HS256' });

console.log('='.repeat(80));
console.log('🔐 TEST JWT TOKEN GENERATED');
console.log('='.repeat(80));
console.log('\n📋 Token Payload:');
console.log(JSON.stringify(payload, null, 2));
console.log('\n🎫 JWT Token:');
console.log(token);
console.log('\n💡 Usage:');
console.log('export TOKEN="' + token + '"');
console.log('curl -H "Authorization: Bearer $TOKEN" http://localhost:3000/api/user/profile');
console.log('\n⏰ Token expires:', new Date(payload.exp * 1000).toLocaleString());
console.log('='.repeat(80));
