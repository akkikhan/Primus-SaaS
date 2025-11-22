import express, { Request, Response } from 'express';
import morgan from 'morgan';
import dotenv from 'dotenv';
import { primusIdentityMiddleware, requireRoles } from '../../../sdk/nodejs/primus-identity-validator/dist';
import type { PrimusUser } from '../../../sdk/nodejs/primus-identity-validator/dist/types';

// Load environment variables
dotenv.config();

const app = express();
const PORT = process.env.PORT || 3000;

// Middleware
app.use(morgan('dev'));
app.use(express.json());

// Configure Primus Identity validation (multi-issuer)
const primusAuth = primusIdentityMiddleware({
  issuers: [
    {
      name: 'AzureAD',
      type: 'oidc',
      issuer: process.env.AZURE_AD_ISSUER ?? '',
      authority: process.env.AZURE_AD_AUTHORITY ?? '',
      audiences: [process.env.API_AUDIENCE ?? '']
    },
    {
      name: 'LocalAuth',
      type: 'jwt',
      issuer: process.env.LOCAL_ISSUER ?? 'http://localhost:4000',
      secret: process.env.LOCAL_SECRET ?? 'local-dev-secret',
      audiences: [process.env.API_AUDIENCE ?? '']
    }
  ],
  clockSkew: 300
});

// Extend Express Request type to include user
declare global {
  namespace Express {
    interface Request {
      user?: PrimusUser;
    }
  }
}

// Public endpoint - no authentication required
app.get('/api/public', (req: Request, res: Response) => {
  res.json({
    message: 'This is a public endpoint accessible without authentication',
    timestamp: new Date().toISOString(),
  });
});

// Protected endpoint - requires authentication
app.get('/api/protected', primusAuth, (req: Request, res: Response) => {
  res.json({
    message: 'This endpoint requires authentication',
    user: {
      userId: req.user?.userId,
      email: req.user?.email,
      name: req.user?.name,
      roles: req.user?.roles,
    },
  });
});

// Admin endpoint - requires Admin role
app.get('/api/admin', primusAuth, requireRoles('Admin'), (req: Request, res: Response) => {
  res.json({
    message: 'This endpoint requires Admin role',
    user: {
      userId: req.user?.userId,
      email: req.user?.email,
      name: req.user?.name,
      roles: req.user?.roles,
    },
  });
});

// Manager or Admin endpoint - requires either role
app.get('/api/management', primusAuth, requireRoles('Manager', 'Admin'), (req: Request, res: Response) => {
  res.json({
    message: 'This endpoint requires Manager or Admin role',
    user: {
      userId: req.user?.userId,
      email: req.user?.email,
      name: req.user?.name,
      roles: req.user?.roles,
    },
  });
});

// Weather endpoint - demonstrates authenticated endpoint with data
interface WeatherForecast {
  date: string;
  temperatureC: number;
  temperatureF: number;
  summary: string;
}

const summaries = [
  'Freezing', 'Bracing', 'Chilly', 'Cool', 'Mild', 'Warm', 'Balmy', 'Hot', 'Sweltering', 'Scorching'
];

app.get('/api/weather', primusAuth, (req: Request, res: Response) => {
  const forecasts: WeatherForecast[] = [];
  
  for (let i = 1; i <= 5; i++) {
    const date = new Date();
    date.setDate(date.getDate() + i);
    
    forecasts.push({
      date: date.toISOString().split('T')[0],
      temperatureC: Math.floor(Math.random() * 75) - 20,
      temperatureF: 0, // Will be calculated
      summary: summaries[Math.floor(Math.random() * summaries.length)],
    });
  }
  
  // Calculate Fahrenheit
  forecasts.forEach(f => {
    f.temperatureF = 32 + Math.floor(f.temperatureC / 0.5556);
  });
  
  console.log(`User ${req.user?.userId} (${req.user?.email}) requested weather forecast`);
  
  res.json({
    forecasts,
    requestedBy: req.user?.email,
  });
});

// Extended weather - requires Admin or Manager role
app.get('/api/weather/extended', primusAuth, requireRoles('Admin', 'Manager'), (req: Request, res: Response) => {
  const forecasts: WeatherForecast[] = [];
  
  for (let i = 1; i <= 14; i++) {
    const date = new Date();
    date.setDate(date.getDate() + i);
    
    forecasts.push({
      date: date.toISOString().split('T')[0],
      temperatureC: Math.floor(Math.random() * 75) - 20,
      temperatureF: 0,
      summary: summaries[Math.floor(Math.random() * summaries.length)],
    });
  }
  
  forecasts.forEach(f => {
    f.temperatureF = 32 + Math.floor(f.temperatureC / 0.5556);
  });
  
  console.log(`User ${req.user?.userId} with roles [${req.user?.roles?.join(', ')}] requested extended forecast`);
  
  res.json({
    forecasts,
    requestedBy: req.user?.email,
    userRoles: req.user?.roles,
  });
});

// Error handling
app.use((err: Error, req: Request, res: Response, next: any) => {
  console.error('Error:', err.message);
  res.status(500).json({
    error: 'Internal server error',
    message: process.env.NODE_ENV === 'development' ? err.message : undefined,
  });
});

// 404 handler
app.use((req: Request, res: Response) => {
  res.status(404).json({
    error: 'Not found',
    path: req.path,
  });
});

// Start server
app.listen(PORT, () => {
  console.log(`\n🚀 Server is running on http://localhost:${PORT}`);
  console.log('\nAvailable endpoints:');
  console.log('  GET /api/public          - Public endpoint (no auth)');
  console.log('  GET /api/protected       - Protected endpoint (requires auth)');
  console.log('  GET /api/admin           - Admin endpoint (requires Admin role)');
  console.log('  GET /api/management      - Management endpoint (requires Manager or Admin role)');
  console.log('  GET /api/weather         - Weather forecast (requires auth)');
  console.log('  GET /api/weather/extended - Extended forecast (requires Admin or Manager role)');
  console.log('\nUse Authorization: Bearer <token> header for protected endpoints\n');
});
