import { FeatureFlagService } from '../src/featureFlagService';
import { FeatureFlagsOptions, FeatureFlagContext } from '../src/types';

describe('FeatureFlagService', () => {
  describe('isEnabled', () => {
    it('should return default value when flag not found', () => {
      const service = new FeatureFlagService({
        defaultValue: false,
        flags: {},
      });

      expect(service.isEnabled('nonExistent')).toBe(false);
    });

    it('should return true when flag is globally enabled', () => {
      const service = new FeatureFlagService({
        flags: {
          testFeature: { enabled: true },
        },
        logging: { logEvaluations: false },
      });

      expect(service.isEnabled('testFeature')).toBe(true);
    });

    it('should return false when flag is globally disabled', () => {
      const service = new FeatureFlagService({
        flags: {
          testFeature: { enabled: false },
        },
        logging: { logEvaluations: false },
      });

      expect(service.isEnabled('testFeature')).toBe(false);
    });

    it('should be case insensitive', () => {
      const service = new FeatureFlagService({
        flags: {
          TestFeature: { enabled: true },
        },
        logging: { logEvaluations: false },
      });

      expect(service.isEnabled('testfeature')).toBe(true);
      expect(service.isEnabled('TESTFEATURE')).toBe(true);
      expect(service.isEnabled('TestFeature')).toBe(true);
    });
  });

  describe('user targeting', () => {
    it('should enable flag for targeted user', () => {
      const service = new FeatureFlagService({
        flags: {
          betaFeature: {
            enabled: false,
            enabledForUsers: ['user123', 'user456'],
          },
        },
        logging: { logEvaluations: false },
      });

      expect(service.isEnabled('betaFeature', 'user123')).toBe(true);
    });

    it('should not enable flag for non-targeted user', () => {
      const service = new FeatureFlagService({
        flags: {
          betaFeature: {
            enabled: false,
            enabledForUsers: ['user123', 'user456'],
          },
        },
        logging: { logEvaluations: false },
      });

      expect(service.isEnabled('betaFeature', 'user789')).toBe(false);
    });
  });

  describe('group targeting', () => {
    it('should enable flag for targeted group', () => {
      const service = new FeatureFlagService({
        flags: {
          adminFeature: {
            enabled: false,
            enabledForGroups: ['admins', 'moderators'],
          },
        },
        logging: { logEvaluations: false },
      });

      const context: FeatureFlagContext = {
        userId: 'user123',
        groups: ['users', 'admins'],
      };

      expect(service.isEnabled('adminFeature', context)).toBe(true);
    });

    it('should not enable flag for non-targeted group', () => {
      const service = new FeatureFlagService({
        flags: {
          adminFeature: {
            enabled: false,
            enabledForGroups: ['admins', 'moderators'],
          },
        },
        logging: { logEvaluations: false },
      });

      const context: FeatureFlagContext = {
        userId: 'user123',
        groups: ['users', 'guests'],
      };

      expect(service.isEnabled('adminFeature', context)).toBe(false);
    });
  });

  describe('time-based activation', () => {
    it('should return false before start time', () => {
      const futureDate = new Date(Date.now() + 86400000).toISOString(); // Tomorrow
      const service = new FeatureFlagService({
        flags: {
          futureFeature: {
            enabled: true,
            startTime: futureDate,
          },
        },
        logging: { logEvaluations: false },
      });

      expect(service.isEnabled('futureFeature')).toBe(false);
    });

    it('should return false after end time', () => {
      const pastDate = new Date(Date.now() - 86400000).toISOString(); // Yesterday
      const service = new FeatureFlagService({
        flags: {
          expiredFeature: {
            enabled: true,
            endTime: pastDate,
          },
        },
        logging: { logEvaluations: false },
      });

      expect(service.isEnabled('expiredFeature')).toBe(false);
    });

    it('should return enabled within time window', () => {
      const pastDate = new Date(Date.now() - 86400000).toISOString();
      const futureDate = new Date(Date.now() + 86400000).toISOString();
      const service = new FeatureFlagService({
        flags: {
          activeFeature: {
            enabled: true,
            startTime: pastDate,
            endTime: futureDate,
          },
        },
        logging: { logEvaluations: false },
      });

      expect(service.isEnabled('activeFeature')).toBe(true);
    });
  });

  describe('percentage rollout', () => {
    it('should always return true for 100% rollout', () => {
      const service = new FeatureFlagService({
        flags: {
          fullRollout: {
            enabled: false,
            rolloutPercentage: 100,
          },
        },
        logging: { logEvaluations: false },
      });

      for (let i = 0; i < 10; i++) {
        expect(service.isEnabled('fullRollout', `user${i}`)).toBe(true);
      }
    });

    it('should always return false for 0% rollout', () => {
      const service = new FeatureFlagService({
        flags: {
          noRollout: {
            enabled: false,
            rolloutPercentage: 0,
          },
        },
        logging: { logEvaluations: false },
      });

      for (let i = 0; i < 10; i++) {
        expect(service.isEnabled('noRollout', `user${i}`)).toBe(false);
      }
    });

    it('should be consistent for the same user', () => {
      const service = new FeatureFlagService({
        flags: {
          partialRollout: {
            enabled: false,
            rolloutPercentage: 50,
          },
        },
        logging: { logEvaluations: false },
      });

      const firstResult = service.isEnabled('partialRollout', 'consistent-user');
      
      for (let i = 0; i < 10; i++) {
        expect(service.isEnabled('partialRollout', 'consistent-user')).toBe(firstResult);
      }
    });
  });

  describe('getAllFlags', () => {
    it('should return all flags with their enabled state', () => {
      const service = new FeatureFlagService({
        flags: {
          feature1: { enabled: true },
          feature2: { enabled: false },
          feature3: { enabled: true },
        },
        logging: { logEvaluations: false },
      });

      const flags = service.getAllFlags();

      expect(flags.size).toBe(3);
      expect(flags.get('feature1')).toBe(true);
      expect(flags.get('feature2')).toBe(false);
      expect(flags.get('feature3')).toBe(true);
    });
  });

  describe('getFlagDefinition', () => {
    it('should return flag definition when exists', () => {
      const service = new FeatureFlagService({
        flags: {
          testFeature: {
            enabled: true,
            description: 'Test description',
            rolloutPercentage: 75,
          },
        },
        logging: { logEvaluations: false },
      });

      const definition = service.getFlagDefinition('testFeature');

      expect(definition).toBeDefined();
      expect(definition?.enabled).toBe(true);
      expect(definition?.description).toBe('Test description');
      expect(definition?.rolloutPercentage).toBe(75);
    });

    it('should return undefined when flag does not exist', () => {
      const service = new FeatureFlagService({
        flags: {},
        logging: { logEvaluations: false },
      });

      const definition = service.getFlagDefinition('nonExistent');

      expect(definition).toBeUndefined();
    });
  });

  describe('evaluate', () => {
    it('should return detailed evaluation result', () => {
      const service = new FeatureFlagService({
        flags: {
          testFeature: { enabled: true },
        },
        logging: { logEvaluations: false },
      });

      const result = service.evaluate('testFeature');

      expect(result.featureName).toBe('testFeature');
      expect(result.enabled).toBe(true);
      expect(result.reason).toBe('GlobalEnabled');
      expect(result.evaluatedAt).toBeInstanceOf(Date);
    });

    it('should return NotFound reason for unknown flag', () => {
      const service = new FeatureFlagService({
        flags: {},
        logging: { logEvaluations: false },
      });

      const result = service.evaluate('unknownFeature');

      expect(result.enabled).toBe(false);
      expect(result.reason).toBe('NotFound');
    });

    it('should return UserTargeted reason when user is in list', () => {
      const service = new FeatureFlagService({
        flags: {
          betaFeature: {
            enabled: false,
            enabledForUsers: ['user123'],
          },
        },
        logging: { logEvaluations: false },
      });

      const result = service.evaluate('betaFeature', 'user123');

      expect(result.enabled).toBe(true);
      expect(result.reason).toBe('UserTargeted');
    });
  });
});
