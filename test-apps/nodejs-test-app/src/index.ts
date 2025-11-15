import express from 'express';
import dotenv from 'dotenv';
import { primusIdentityMiddleware, requireRoles } from '../../../sdk/nodejs/primus-identity-validator/dist';
import type { PrimusUser } from '../../../sdk/nodejs/primus-identity-validator/dist/types';

dotenv.config();

const app = express();
const port = process.env.PORT || 3001;

// Configure Primus Identity Middleware
const primusAuth = primusIdentityMiddleware({
  portalUrl: process.env.PRIMUS_PORTAL_URL || 'https://localhost:7001',
  clientId: process.env.PRIMUS_CLIENT_ID || 'test-client-123',
  clientSecret: process.env.PRIMUS_CLIENT_SECRET || 'test-secret-456',
  jwtSecret: process.env.PRIMUS_JWT_SECRET || 'test-jwt-secret-key-with-at-least-32-characters-long',
});

// Extend Express Request type
declare global {
  namespace Express {
    interface Request {
      user?: PrimusUser;
    }
  }
}

app.use(express.json());

// Health check
app.get('/api/health', (req, res) => {
  res.json({
    status: 'healthy',
    message: 'Primus SDK Test App (Node.js) is running',
    timestamp: new Date().toISOString(),
  });
});

// Public endpoint
app.get('/api/public', (req, res) => {
  res.json({
    message: 'This is a public endpoint - no authentication required',
  });
});

// Protected endpoint
app.get('/api/protected', primusAuth, (req, res) => {
  res.json({
    message: 'Successfully authenticated!',
    user: {
      userId: req.user?.userId,
      email: req.user?.email,
      name: req.user?.name,
      roles: req.user?.roles || [],
    },
    timestamp: new Date().toISOString(),
  });
});

// Admin only endpoint
app.get('/api/admin', primusAuth, requireRoles('Admin'), (req, res) => {
  res.json({
    message: 'Admin access granted!',
    user: {
      userId: req.user?.userId,
      email: req.user?.email,
      roles: req.user?.roles || [],
    },
  });
});

// Manager or Admin endpoint
app.get('/api/manager', primusAuth, requireRoles('Manager', 'Admin'), (req, res) => {
  res.json({
    message: 'Manager or Admin access granted!',
    user: {
      userId: req.user?.userId,
      email: req.user?.email,
      roles: req.user?.roles || [],
    },
  });
});

// Error handler
app.use((err: any, req: express.Request, res: express.Response, next: express.NextFunction) => {
  console.error('Error:', err.message);
  res.status(err.status || 500).json({
    error: err.message || 'Internal server error',
  });
});

app.listen(port, () => {
  console.log('🚀 Primus SDK Test Application (Node.js) Starting...');
  console.log(`📦 Using Primus Portal: ${process.env.PRIMUS_PORTAL_URL || 'https://localhost:7001'}`);
  console.log(`🔑 Client ID: ${process.env.PRIMUS_CLIENT_ID || 'test-client-123'}`);
  console.log(`✅ Primus Identity Validator configured successfully`);
  console.log(`🌐 Server listening on http://localhost:${port}`);
  console.log(`\nTest endpoints:`);
  console.log(`  GET http://localhost:${port}/api/health - Health check`);
  console.log(`  GET http://localhost:${port}/api/public - Public (no auth)`);
  console.log(`  GET http://localhost:${port}/api/protected - Protected (auth required)`);
  console.log(`  GET http://localhost:${port}/api/admin - Admin only`);
  console.log(`  GET http://localhost:${port}/api/manager - Manager or Admin`);
});
