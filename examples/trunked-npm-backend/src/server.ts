import express, { Request, Response, NextFunction } from 'express';
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

app.get('/health', (_req, res) => {
  res.json({
    status: 'ok',
    mode: validationMode,
    needsLocalSecret,
    needsAzureTenant
  });
});

app.get('/api/public', (_req, res) => {
  res.json({
    message: 'This route does not require a Primus token',
    timestamp: new Date().toISOString()
  });
});

app.get('/api/profile', primusAuth, (req, res) => {
  res.json({
    user: req.primusUser,
    message: `Hello ${req.primusUser?.name ?? 'anonymous user'}`
  });
});

app.get('/api/admin', primusAuth, requireRoles('Admin'), (req, res) => {
  res.json({
    message: 'Admin route hit successfully',
    user: req.primusUser
  });
});

app.get('/api/management', primusAuth, requireRoles('Manager', 'Admin'), (req, res) => {
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
