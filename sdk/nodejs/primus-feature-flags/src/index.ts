// Types
export {
  FeatureFlagsOptions,
  FeatureFlagDefinition,
  FeatureFlagContext,
  FeatureFlagEvaluationResult,
  FeatureFlagLoggingOptions,
} from './types';

// Service
export { FeatureFlagService } from './featureFlagService';

// Express integration
export {
  requireFeature,
  useFeatureFlags,
  FeatureFlagMiddlewareOptions,
} from './express';
