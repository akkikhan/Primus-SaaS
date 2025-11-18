import express, { Request, Response } from 'express';
import dotenv from 'dotenv';
import cors from 'cors';
import { 
  primusIdentityMiddleware, 
  requireRoles, 
  ValidationMode,
  PrimusUser 
} from 'primus-identity-validator';

// Load environment variables
dotenv.config();

const app = express();
const PORT = Number(process.env.PORT) || 3001;

// Middleware
app.use(cors({
  origin: process.env.CORS_ORIGIN?.split(',') || '*',
  credentials: true
}));
app.use(express.json());

// Determine validation mode from environment
const getValidationMode = (): ValidationMode => {
  const mode = process.env.VALIDATION_MODE?.toLowerCase();
  switch (mode) {
    case 'azuread':
      return ValidationMode.AzureAd;
    case 'hybrid':
      return ValidationMode.Hybrid;
    case 'local':
    default:
      return ValidationMode.Local;
  }
};

// Configure Primus authentication middleware
const primusAuth = primusIdentityMiddleware({
  portalUrl: process.env.PRIMUS_PORTAL_URL || 'https://portal.primus-saas.com',
  clientId: process.env.PRIMUS_CLIENT_ID!,
  clientSecret: process.env.PRIMUS_CLIENT_SECRET!,
  jwtSecret: process.env.PRIMUS_CLIENT_SECRET!, // For Local mode, jwtSecret is the same as clientSecret
  mode: getValidationMode(),
  tenantId: process.env.AZURE_AD_TENANT_ID,
  jwksCacheTtl: process.env.JWKS_CACHE_TTL ? parseInt(process.env.JWKS_CACHE_TTL) : 24,
  clockSkew: 300 // 5 minutes tolerance
});

// Extend Express Request type to include primusUser
declare global {
  namespace Express {
    interface Request {
      primusUser?: PrimusUser;
    }
  }
}

// ============================================
// PUBLIC ROUTES (No authentication required)
// ============================================

app.get('/', (req: Request, res: Response) => {
  res.json({
    message: 'Primus Auth Test Application',
    version: '1.0.0',
    endpoints: {
      public: [
        'GET /',
        'GET /api/public',
        'GET /api/health'
      ],
      protected: [
        'GET /api/user/profile',
        'GET /api/user/permissions'
      ],
      admin: [
        'GET /api/admin/settings',
        'GET /api/admin/users'
      ],
      manager: [
        'GET /api/manager/reports',
        'GET /api/manager/team'
      ]
    },
    configuration: {
      validationMode: process.env.VALIDATION_MODE || 'Local',
      portalUrl: process.env.PRIMUS_PORTAL_URL,
      clientId: process.env.PRIMUS_CLIENT_ID,
      azureTenantId: process.env.AZURE_AD_TENANT_ID ? '***configured***' : undefined
    }
  });
});

app.get('/api/public', (req: Request, res: Response) => {
  res.json({
    message: 'This is a public endpoint - no authentication required',
    timestamp: new Date().toISOString()
  });
});

app.get('/api/health', (req: Request, res: Response) => {
  res.json({
    status: 'healthy',
    uptime: process.uptime(),
    timestamp: new Date().toISOString()
  });
});

// ============================================
// PROTECTED ROUTES (Authentication required)
// ============================================

app.get('/api/user/profile', primusAuth, (req: Request, res: Response) => {
  const user = req.primusUser;
  res.json({
    message: 'User profile retrieved successfully',
    user: {
      userId: user?.userId,
      email: user?.email,
      name: user?.name,
      roles: user?.roles
    }
  });
});

app.get('/api/user/permissions', primusAuth, (req: Request, res: Response) => {
  const user = req.primusUser;
  res.json({
    message: 'User permissions retrieved',
    userId: user?.userId,
    roles: user?.roles || [],
    permissions: {
      canRead: true,
      canWrite: user?.roles?.includes('Admin') || user?.roles?.includes('Manager'),
      canDelete: user?.roles?.includes('Admin')
    }
  });
});

// ============================================
// ADMIN ROUTES (Admin role required)
// ============================================

app.get('/api/admin/settings', primusAuth, requireRoles('Admin'), (req: Request, res: Response) => {
  res.json({
    message: 'Admin settings retrieved',
    settings: {
      siteName: 'Primus Test App',
      maintenanceMode: false,
      debugEnabled: process.env.NODE_ENV === 'development'
    }
  });
});

app.get('/api/admin/users', primusAuth, requireRoles('Admin'), (req: Request, res: Response) => {
  res.json({
    message: 'User list retrieved (Admin only)',
    users: [
      { id: 1, name: 'John Doe', role: 'User' },
      { id: 2, name: 'Jane Smith', role: 'Manager' },
      { id: 3, name: 'Admin User', role: 'Admin' }
    ]
  });
});

// ============================================
// MANAGER ROUTES (Manager or Admin role required)
// ============================================

app.get('/api/manager/reports', primusAuth, requireRoles('Manager', 'Admin'), (req: Request, res: Response) => {
  res.json({
    message: 'Manager reports retrieved',
    reports: [
      { id: 1, name: 'Q4 Sales Report', date: '2024-12-31' },
      { id: 2, name: 'Team Performance', date: '2024-12-15' }
    ]
  });
});

app.get('/api/manager/team', primusAuth, requireRoles('Manager', 'Admin'), (req: Request, res: Response) => {
  const user = req.primusUser;
  res.json({
    message: 'Team members retrieved',
    manager: user?.name,
    team: [
      { id: 1, name: 'Developer 1', position: 'Senior Developer' },
      { id: 2, name: 'Developer 2', position: 'Junior Developer' },
      { id: 3, name: 'Designer 1', position: 'UI/UX Designer' }
    ]
  });
});

// ============================================
// ERROR HANDLING
// ============================================

app.use((req: Request, res: Response) => {
  res.status(404).json({
    error: 'Not Found',
    message: `Route ${req.method} ${req.path} not found`,
    availableEndpoints: '/'
  });
});

app.use((err: any, req: Request, res: Response, next: any) => {
  console.error('Error:', err);
  res.status(err.status || 500).json({
    error: err.message || 'Internal Server Error',
    ...(process.env.NODE_ENV === 'development' && { stack: err.stack })
  });
});

// ============================================
// SERVER STARTUP
// ============================================

const server = app.listen(PORT, () => {
  console.log('='.repeat(60));
  console.log('🚀 Primus Auth Test Application');
  console.log('='.repeat(60));
  console.log(`📍 Server: http://localhost:${PORT}`);
  console.log(`🔒 Validation Mode: ${process.env.VALIDATION_MODE || 'Local'}`);
  console.log(`🏢 Portal URL: ${process.env.PRIMUS_PORTAL_URL}`);
  console.log(`🆔 Client ID: ${process.env.PRIMUS_CLIENT_ID}`);
  if (process.env.AZURE_AD_TENANT_ID) {
    console.log(`☁️  Azure AD Tenant: ${process.env.AZURE_AD_TENANT_ID}`);
  }
  console.log('='.repeat(60));
  console.log('\n📋 Available Endpoints:');
  console.log('  Public:    GET /');
  console.log('  Public:    GET /api/public');
  console.log('  Public:    GET /api/health');
  console.log('  Protected: GET /api/user/profile');
  console.log('  Protected: GET /api/user/permissions');
  console.log('  Admin:     GET /api/admin/settings');
  console.log('  Admin:     GET /api/admin/users');
  console.log('  Manager:   GET /api/manager/reports');
  console.log('  Manager:   GET /api/manager/team');
  console.log('\n💡 Test with:');
  console.log('  curl http://localhost:' + PORT + '/api/public');
  console.log('  curl -H "Authorization: Bearer YOUR_TOKEN" http://localhost:' + PORT + '/api/user/profile');
  console.log('='.repeat(60));
});

server.on('error', (err: any) => {
  console.error('❌ Server Error:', err.message);
  if (err.code === 'EADDRINUSE') {
    console.error(`Port ${PORT} is already in use. Please change PORT in .env file.`);
  }
  process.exit(1);
});

process.on('SIGTERM', () => {
  console.log('\n🛑 SIGTERM received, shutting down gracefully');
  server.close(() => {
    console.log('✅ Server closed');
    process.exit(0);
  });
});
