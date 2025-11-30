# @primus-saas/feature-flags

Lightweight feature flag management for Node.js applications. Supports in-memory, file-based, and environment-based providers with percentage rollouts and user targeting.

[![npm version](https://img.shields.io/npm/v/@primus-saas/feature-flags.svg)](https://www.npmjs.com/package/@primus-saas/feature-flags)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## Features

- **Multiple Providers**: In-memory, JSON file, or environment variables
- **Percentage Rollouts**: Gradual feature rollouts with consistent hashing
- **User Targeting**: Enable features for specific users or groups
- **Time-Based Activation**: Schedule feature availability windows
- **Express Integration**: Middleware for route-level feature gating
- **TypeScript Support**: Full type definitions included

## Installation

```bash
npm install @primus-saas/feature-flags
```

## Quick Start

### Basic Usage

```typescript
import { FeatureFlagService } from '@primus-saas/feature-flags';

const featureFlags = new FeatureFlagService({
  flags: {
    newDashboard: {
      enabled: true,
      description: 'Enable the new dashboard UI',
    },
    betaFeature: {
      enabled: false,
      rolloutPercentage: 25,
      enabledForGroups: ['beta-testers'],
    },
  },
});

// Simple check
if (featureFlags.isEnabled('newDashboard')) {
  console.log('New dashboard is enabled!');
}

// User-specific check
if (featureFlags.isEnabled('betaFeature', 'user123')) {
  console.log('User is in beta rollout');
}

// Context-based check
const context = {
  userId: 'user123',
  email: 'user@example.com',
  groups: ['admins', 'beta-testers'],
};

if (featureFlags.isEnabled('betaFeature', context)) {
  console.log('Feature enabled for this user');
}
```

### Express Integration

```typescript
import express from 'express';
import {
  FeatureFlagService,
  requireFeature,
  useFeatureFlags,
} from '@primus-saas/feature-flags';

const app = express();

const featureFlags = new FeatureFlagService({
  flags: {
    newApi: { enabled: true },
    betaEndpoint: { enabled: false, rolloutPercentage: 50 },
  },
});

// Add feature flags to all requests
app.use(useFeatureFlags(featureFlags));

// Protect specific routes
app.get(
  '/api/v2/dashboard',
  requireFeature(featureFlags, { featureName: 'newApi' }),
  (req, res) => {
    res.json({ version: 'v2', data: 'New API response' });
  }
);

// Access in route handlers
app.get('/api/features', (req, res) => {
  const flags = req.featureFlags?.getAllFlags();
  res.json(Object.fromEntries(flags || []));
});
```

## Configuration Options

### In-Memory Provider (Default)

```typescript
const featureFlags = new FeatureFlagService({
  provider: 'memory',
  defaultValue: false,
  flags: {
    myFeature: {
      enabled: true,
      description: 'My awesome feature',
      rolloutPercentage: 50,
      enabledForUsers: ['user1@example.com', 'user2@example.com'],
      enabledForGroups: ['admins', 'beta-testers'],
      startTime: '2024-01-01T00:00:00Z',
      endTime: '2024-12-31T23:59:59Z',
    },
  },
});
```

### JSON File Provider

```typescript
const featureFlags = new FeatureFlagService({
  provider: 'json',
  jsonFilePath: './config/featureflags.json',
});
```

**featureflags.json:**
```json
{
  "newDashboard": {
    "enabled": true,
    "description": "New dashboard UI",
    "rolloutPercentage": 100
  },
  "betaFeature": {
    "enabled": false,
    "rolloutPercentage": 25,
    "enabledForGroups": ["beta-testers"]
  }
}
```

### Environment Variable Provider

```typescript
const featureFlags = new FeatureFlagService({
  provider: 'env',
  envPrefix: 'FEATURE_', // Default
});

// Set flags via environment variables:
// FEATURE_NEW_DASHBOARD=true
// FEATURE_BETA_MODE=false
```

## Feature Flag Evaluation

### Evaluation Order

Feature flags are evaluated in the following order:

1. **Time-Based**: Check if current time is within StartTime/EndTime window
2. **User Targeting**: Check if user ID is in enabledForUsers list
3. **Group Targeting**: Check if user belongs to any enabledForGroups
4. **Percentage Rollout**: Use consistent hashing to determine if user is in rollout
5. **Global Enabled**: Fall back to the enabled flag

### Detailed Evaluation

```typescript
const result = featureFlags.evaluate('myFeature', { userId: 'user123' });

console.log(result);
// {
//   featureName: 'myFeature',
//   enabled: true,
//   reason: 'UserTargeted',
//   evaluatedAt: Date
// }
```

## API Reference

### FeatureFlagService

| Method | Description |
|--------|-------------|
| `isEnabled(featureName, context?)` | Check if feature is enabled |
| `isEnabledAsync(featureName, context?)` | Async check with cache refresh |
| `getAllFlags()` | Get all feature flags and states |
| `getFlagDefinition(featureName)` | Get feature flag definition |
| `evaluate(featureName, context?)` | Get detailed evaluation result |
| `refreshAsync()` | Force refresh from provider |

### FeatureFlagDefinition

| Property | Type | Description |
|----------|------|-------------|
| `enabled` | boolean | Global enabled state |
| `description` | string? | Human-readable description |
| `rolloutPercentage` | number? | Percentage of users (0-100) |
| `enabledForUsers` | string[]? | User IDs always enabled |
| `enabledForGroups` | string[]? | Group names always enabled |
| `startTime` | string? | Activation start (ISO 8601) |
| `endTime` | string? | Activation end (ISO 8601) |
| `metadata` | object? | Custom key-value pairs |

## Logging

```typescript
const featureFlags = new FeatureFlagService({
  flags: { /* ... */ },
  logging: {
    logEvaluations: true,
    includeUserContext: false, // Set true to include userId in logs
    logger: (message) => console.log(`[FeatureFlags] ${message}`),
  },
});
```

## Best Practices

1. **Use meaningful flag names**: `newCheckoutFlow` not `feature1`
2. **Set descriptions**: Document what each flag controls
3. **Clean up old flags**: Remove flags after full rollout
4. **Use percentage rollouts**: Gradual rollouts reduce risk
5. **Keep targeting simple**: Too many rules become hard to manage

## Related Packages

- `@primus-saas/identity-validator` - Multi-issuer JWT validation
- `@primus-saas/logging` - Structured logging

## License

MIT License - see [LICENSE](LICENSE) for details.
