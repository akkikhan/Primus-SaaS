/**
 * Configuration options for PrimusSaaS Feature Flags.
 */
export interface FeatureFlagsOptions {
  /**
   * The provider type to use for feature flag storage.
   * Default is 'memory'.
   */
  provider?: 'memory' | 'json' | 'env';

  /**
   * Path to the JSON configuration file when using 'json' provider.
   */
  jsonFilePath?: string;

  /**
   * Environment variable prefix when using 'env' provider.
   * Default is 'FEATURE_'.
   */
  envPrefix?: string;

  /**
   * Cache duration for feature flag values in milliseconds.
   * Default is 30000 (30 seconds).
   */
  cacheDurationMs?: number;

  /**
   * Default value to return when a feature flag is not found.
   * Default is false.
   */
  defaultValue?: boolean;

  /**
   * In-memory feature flag definitions.
   */
  flags?: Record<string, FeatureFlagDefinition>;

  /**
   * Logging options for feature flag evaluation.
   */
  logging?: FeatureFlagLoggingOptions;
}

/**
 * Definition of a single feature flag.
 */
export interface FeatureFlagDefinition {
  /**
   * Whether the feature flag is enabled globally.
   */
  enabled: boolean;

  /**
   * Optional description of the feature flag.
   */
  description?: string;

  /**
   * Percentage rollout (0-100). If set, flag is enabled for this percentage of users.
   */
  rolloutPercentage?: number;

  /**
   * List of user identifiers that should always have this flag enabled.
   */
  enabledForUsers?: string[];

  /**
   * List of group names that should always have this flag enabled.
   */
  enabledForGroups?: string[];

  /**
   * Start time for time-based activation (ISO 8601 string).
   */
  startTime?: string;

  /**
   * End time for time-based activation (ISO 8601 string).
   */
  endTime?: string;

  /**
   * Custom metadata for the feature flag.
   */
  metadata?: Record<string, string>;
}

/**
 * Logging options for feature flag evaluation.
 */
export interface FeatureFlagLoggingOptions {
  /**
   * Whether to log feature flag evaluations.
   * Default is true.
   */
  logEvaluations?: boolean;

  /**
   * Whether to include user context in logs (may contain PII).
   * Default is false.
   */
  includeUserContext?: boolean;

  /**
   * Custom logger function.
   */
  logger?: (message: string, ...args: any[]) => void;
}

/**
 * Context for feature flag evaluation with targeting.
 */
export interface FeatureFlagContext {
  /**
   * User identifier for targeting.
   */
  userId?: string;

  /**
   * User email for targeting.
   */
  email?: string;

  /**
   * Groups the user belongs to for targeting.
   */
  groups?: string[];

  /**
   * Custom attributes for targeting.
   */
  attributes?: Record<string, string>;
}

/**
 * Result of a feature flag evaluation.
 */
export interface FeatureFlagEvaluationResult {
  /**
   * The feature flag name.
   */
  featureName: string;

  /**
   * Whether the feature is enabled.
   */
  enabled: boolean;

  /**
   * Reason for the evaluation result.
   */
  reason: string;

  /**
   * Timestamp of the evaluation.
   */
  evaluatedAt: Date;
}
