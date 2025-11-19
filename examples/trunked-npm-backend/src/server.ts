import express, { Request, Response, NextFunction, RequestHandler } from 'express';
import dotenv from 'dotenv';
import {
  primusIdentityMiddleware,
  requireRoles,
  ValidationMode
} from 'primus-identity-validator';

dotenv.config();

const app = express();
app.use(express.json());

const validationMode = resolveValidationMode(process.env.PRIMUS_VALIDATION_MODE);
const needsLocalSecret =
  validationMode === ValidationMode.Local || validationMode === ValidationMode.Hybrid;
const needsAzureTenant =
  validationMode === ValidationMode.AzureAd || validationMode === ValidationMode.Hybrid;
const authEnforced = process.env.PRIMUS_ENFORCE_AUTH !== 'false';

const primusAuth = primusIdentityMiddleware({
  portalUrl: requireEnv('PRIMUS_PORTAL_URL'),
  clientId: requireEnv('PRIMUS_CLIENT_ID'),
  clientSecret: requireEnv('PRIMUS_CLIENT_SECRET'),
  mode: validationMode,
  jwtSecret: needsLocalSecret ? requireEnv('PRIMUS_JWT_SECRET') : undefined,
  tenantId: needsAzureTenant ? requireEnv('PRIMUS_AZURE_TENANT_ID') : undefined,
  jwksCacheTtl: Number(process.env.PRIMUS_JWKS_CACHE_TTL ?? 24),
  clockSkew: Number(process.env.PRIMUS_CLOCK_SKEW ?? 300),
  validateLifetime: process.env.PRIMUS_VALIDATE_LIFETIME !== 'false',
  issuer: process.env.PRIMUS_ISSUER,
  audience: process.env.PRIMUS_AUDIENCE
});

const enforceAuth: RequestHandler = (req, res, next) => {
  if (!authEnforced) {
    return next();
  }
  return primusAuth(req, res, next);
};

const requireRolesIfEnabled = (...roles: string[]): RequestHandler => {
  if (!authEnforced) {
    return (_req, _res, next) => next();
  }

  return requireRoles(...roles);
};

const dashboardMetrics = {
  activeUsers: 1284,
  deploymentsToday: 4,
  serviceHealth: 'Operational',
  latencyMs: 182
};

const applications = [
  { id: 'folio', name: 'Folio Manager', status: 'Running', lastDeployment: '2025-02-28T13:05:00Z' },
  { id: 'quotas', name: 'Quota Service', status: 'Running', lastDeployment: '2025-02-28T08:22:00Z' },
  { id: 'ledger', name: 'Ledger API', status: 'Warning', lastDeployment: '2025-02-27T18:10:00Z' }
];

const releaseTimeline = [
  { module: 'Portal Frontend', version: '2.6.0', status: 'In QA' },
  { module: 'Identity Validator', version: '1.0.0', status: 'Released' },
  { module: 'Reporting Engine', version: '0.9.5', status: 'Building' }
];

const notifications = [
  {
    id: 'notify-1',
    message: 'Ledger API latency exceeded 200ms threshold',
    severity: 'warning',
    createdAt: '2025-02-28T11:45:00Z'
  },
  {
    id: 'notify-2',
    message: 'Portal Frontend deployment scheduled for 18:00 UTC',
    severity: 'info',
    createdAt: '2025-02-28T09:00:00Z'
  },
  {
    id: 'notify-3',
    message: 'Quota Service patch requires admin approval',
    severity: 'info',
    createdAt: '2025-02-27T21:12:00Z'
  }
];

app.get('/health', (_req, res) => {
  res.json({
    status: 'ok',
    mode: validationMode,
    needsLocalSecret,
    needsAzureTenant,
    authEnforced
  });
});

app.get('/api/public', (_req, res) => {
  res.json({
    message: 'This route does not require a Primus token',
    timestamp: new Date().toISOString()
  });
});

app.get('/api/dashboard', enforceAuth, (req, res) => {
  res.json({
    metrics: dashboardMetrics,
    applications,
    releases: releaseTimeline,
    authenticated: authEnforced,
    primusUser: req.primusUser ?? null
  });
});

app.get('/api/notifications', enforceAuth, (_req, res) => {
  res.json({
    notifications
  });
});

app.get('/api/profile', enforceAuth, (req, res) => {
  res.json({
    user: req.primusUser,
    message: `Hello ${req.primusUser?.name ?? 'anonymous user'}`
  });
});

app.get('/api/admin', enforceAuth, requireRolesIfEnabled('Admin'), (req, res) => {
  res.json({
    message: 'Admin route hit successfully',
    user: req.primusUser
  });
});

app.get('/api/management', enforceAuth, requireRolesIfEnabled('Manager', 'Admin'), (req, res) => {
  res.json({
    message: 'Managers and Admins may view this content',
    roles: req.primusUser?.roles ?? []
  });
});

app.use((err: Error, _req: Request, res: Response, _next: NextFunction) => {
  if (err.name === 'UnauthorizedError') {
    res.status(401).json({ error: 'Invalid or missing Primus token', details: err.message });
    return;
  }

  res.status(500).json({ error: 'Unexpected server error', details: err.message });
});

const port = Number(process.env.PORT ?? 4000);
app.listen(port, () => {
  console.log(`Trunked Primus backend ready at http://localhost:${port}`);
});

function resolveValidationMode(value?: string): ValidationMode {
  if (!value) {
    return ValidationMode.Local;
  }

  const normalized = value.toLowerCase();
  switch (normalized) {
    case 'azuread':
    case 'azure':
      return ValidationMode.AzureAd;
    case 'hybrid':
      return ValidationMode.Hybrid;
    default:
      return ValidationMode.Local;
  }
}

function requireEnv(name: string): string {
  const value = process.env[name];
  if (!value) {
    throw new Error(`Missing required environment variable: ${name}`);
  }

  return value;
}
