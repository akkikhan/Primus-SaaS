import * as crypto from 'crypto';
import {
  FeatureFlagsOptions,
  FeatureFlagDefinition,
  FeatureFlagContext,
  FeatureFlagEvaluationResult,
} from './types';

/**
 * Feature flag service for evaluating feature flags.
 */
export class FeatureFlagService {
  private options: Required<FeatureFlagsOptions>;
  private flags: Map<string, FeatureFlagDefinition>;
  private lastRefresh: number = 0;

  constructor(options: FeatureFlagsOptions = {}) {
    this.options = {
      provider: options.provider ?? 'memory',
      jsonFilePath: options.jsonFilePath ?? '',
      envPrefix: options.envPrefix ?? 'FEATURE_',
      cacheDurationMs: options.cacheDurationMs ?? 30000,
      defaultValue: options.defaultValue ?? false,
      flags: options.flags ?? {},
      logging: {
        logEvaluations: options.logging?.logEvaluations ?? true,
        includeUserContext: options.logging?.includeUserContext ?? false,
        logger: options.logging?.logger ?? console.log,
      },
    };

    this.flags = new Map(
      Object.entries(this.options.flags).map(([k, v]) => [k.toLowerCase(), v])
    );

    this.loadProvider();
  }

  private loadProvider(): void {
    switch (this.options.provider) {
      case 'json':
        this.loadFromJsonFile();
        break;
      case 'env':
        this.loadFromEnv();
        break;
      default:
        // In-memory already loaded from options
        break;
    }
    this.lastRefresh = Date.now();
  }

  private loadFromJsonFile(): void {
    if (!this.options.jsonFilePath) {
      throw new Error('jsonFilePath must be configured when using json provider');
    }

    try {
      // eslint-disable-next-line @typescript-eslint/no-var-requires
      const fs = require('fs');
      const data = fs.readFileSync(this.options.jsonFilePath, 'utf-8');
      const flags = JSON.parse(data) as Record<string, FeatureFlagDefinition>;

      this.flags = new Map(
        Object.entries(flags).map(([k, v]) => [k.toLowerCase(), v])
      );
    } catch (error) {
      this.log(`Failed to load feature flags from ${this.options.jsonFilePath}: ${error}`);
    }
  }

  private loadFromEnv(): void {
    const prefix = this.options.envPrefix.toUpperCase();
    const envFlags: Record<string, FeatureFlagDefinition> = {};

    for (const [key, value] of Object.entries(process.env)) {
      if (key.startsWith(prefix) && value !== undefined) {
        const flagName = key.slice(prefix.length).toLowerCase();
        envFlags[flagName] = {
          enabled: value.toLowerCase() === 'true' || value === '1',
        };
      }
    }

    this.flags = new Map(Object.entries(envFlags));
  }

  /**
   * Checks if a feature flag is enabled.
   * @param featureName The name of the feature flag.
   * @param context Optional evaluation context.
   * @returns True if the feature is enabled, false otherwise.
   */
  isEnabled(featureName: string, context?: FeatureFlagContext | string): boolean {
    if (!featureName) {
      throw new Error('Feature name cannot be null or empty');
    }

    // Normalize context
    const ctx: FeatureFlagContext =
      typeof context === 'string' ? { userId: context } : context ?? {};

    const definition = this.flags.get(featureName.toLowerCase());

    if (!definition) {
      this.logEvaluation(featureName, this.options.defaultValue, 'NotFound', ctx);
      return this.options.defaultValue;
    }

    return this.evaluateFlag(featureName, definition, ctx);
  }

  /**
   * Asynchronously checks if a feature flag is enabled.
   * @param featureName The name of the feature flag.
   * @param context Optional evaluation context.
   * @returns Promise resolving to true if the feature is enabled.
   */
  async isEnabledAsync(
    featureName: string,
    context?: FeatureFlagContext | string
  ): Promise<boolean> {
    // Refresh if cache expired
    if (Date.now() - this.lastRefresh > this.options.cacheDurationMs) {
      await this.refreshAsync();
    }

    return this.isEnabled(featureName, context);
  }

  /**
   * Gets all available feature flags.
   * @returns Map of feature flag names and their enabled states.
   */
  getAllFlags(): Map<string, boolean> {
    const result = new Map<string, boolean>();
    for (const [name, def] of this.flags) {
      result.set(name, def.enabled);
    }
    return result;
  }

  /**
   * Gets the definition of a specific feature flag.
   * @param featureName The name of the feature flag.
   * @returns The feature flag definition, or undefined if not found.
   */
  getFlagDefinition(featureName: string): FeatureFlagDefinition | undefined {
    return this.flags.get(featureName.toLowerCase());
  }

  /**
   * Forces a refresh of feature flags from the provider.
   */
  async refreshAsync(): Promise<void> {
    this.loadProvider();
  }

  /**
   * Evaluates a feature flag with full result details.
   * @param featureName The name of the feature flag.
   * @param context Optional evaluation context.
   * @returns Full evaluation result with reason.
   */
  evaluate(
    featureName: string,
    context?: FeatureFlagContext | string
  ): FeatureFlagEvaluationResult {
    const ctx: FeatureFlagContext =
      typeof context === 'string' ? { userId: context } : context ?? {};

    const definition = this.flags.get(featureName.toLowerCase());

    if (!definition) {
      return {
        featureName,
        enabled: this.options.defaultValue,
        reason: 'NotFound',
        evaluatedAt: new Date(),
      };
    }

    const { enabled, reason } = this.evaluateFlagWithReason(
      featureName,
      definition,
      ctx
    );

    return {
      featureName,
      enabled,
      reason,
      evaluatedAt: new Date(),
    };
  }

  private evaluateFlag(
    featureName: string,
    definition: FeatureFlagDefinition,
    context: FeatureFlagContext
  ): boolean {
    const { enabled, reason } = this.evaluateFlagWithReason(
      featureName,
      definition,
      context
    );
    this.logEvaluation(featureName, enabled, reason, context);
    return enabled;
  }

  private evaluateFlagWithReason(
    featureName: string,
    definition: FeatureFlagDefinition,
    context: FeatureFlagContext
  ): { enabled: boolean; reason: string } {
    // Check time-based activation
    const now = new Date();

    if (definition.startTime) {
      const startTime = new Date(definition.startTime);
      if (now < startTime) {
        return { enabled: false, reason: 'BeforeStartTime' };
      }
    }

    if (definition.endTime) {
      const endTime = new Date(definition.endTime);
      if (now > endTime) {
        return { enabled: false, reason: 'AfterEndTime' };
      }
    }

    // Check user targeting
    if (context.userId && definition.enabledForUsers?.length) {
      const userIdLower = context.userId.toLowerCase();
      if (
        definition.enabledForUsers.some(
          (u) => u.toLowerCase() === userIdLower
        )
      ) {
        return { enabled: true, reason: 'UserTargeted' };
      }
    }

    // Check group targeting
    if (context.groups?.length && definition.enabledForGroups?.length) {
      const contextGroupsLower = context.groups.map((g) => g.toLowerCase());
      for (const group of definition.enabledForGroups) {
        if (contextGroupsLower.includes(group.toLowerCase())) {
          return { enabled: true, reason: 'GroupTargeted' };
        }
      }
    }

    // Check percentage rollout
    if (
      definition.rolloutPercentage !== undefined &&
      definition.rolloutPercentage > 0 &&
      context.userId
    ) {
      const isInRollout = this.isInRolloutPercentage(
        featureName,
        context.userId,
        definition.rolloutPercentage
      );
      return {
        enabled: isInRollout,
        reason: `Rollout_${definition.rolloutPercentage}%`,
      };
    }

    // Fall back to global enabled state
    return { enabled: definition.enabled, reason: 'GlobalEnabled' };
  }

  private isInRolloutPercentage(
    featureName: string,
    userId: string,
    percentage: number
  ): boolean {
    if (percentage <= 0) return false;
    if (percentage >= 100) return true;

    const key = `${featureName}:${userId}`;
    const hash = this.getConsistentHash(key);
    const bucket = Math.abs(hash % 100);
    return bucket < percentage;
  }

  private getConsistentHash(input: string): number {
    const hash = crypto.createHash('sha256').update(input).digest();
    return hash.readInt32BE(0);
  }

  private logEvaluation(
    featureName: string,
    result: boolean,
    reason: string,
    context: FeatureFlagContext
  ): void {
    if (!this.options.logging.logEvaluations) return;

    const logger = this.options.logging.logger;
    if (this.options.logging.includeUserContext) {
      logger(
        `Feature flag '${featureName}' evaluated to ${result}. Reason: ${reason}. UserId: ${context.userId ?? 'anonymous'}`
      );
    } else {
      logger(
        `Feature flag '${featureName}' evaluated to ${result}. Reason: ${reason}`
      );
    }
  }

  private log(message: string): void {
    if (this.options.logging.logEvaluations) {
      this.options.logging.logger(message);
    }
  }
}
