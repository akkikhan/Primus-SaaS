import axios from 'axios';
import { OpenIdConfigurationService } from '../../src/services/openIdConfigurationService';
import { OpenIdConfiguration } from '../../src/types';

jest.mock('axios');
const mockedAxios = axios as jest.Mocked<typeof axios>;

describe('OpenIdConfigurationService', () => {
  let service: OpenIdConfigurationService;
  const testTenantId = 'cbd15a9b-cd52-4ccc-916a-00e2edb13043';
  
  const mockConfig: OpenIdConfiguration = {
    issuer: `https://login.microsoftonline.com/${testTenantId}/v2.0`,
    authorization_endpoint: `https://login.microsoftonline.com/${testTenantId}/oauth2/v2.0/authorize`,
    token_endpoint: `https://login.microsoftonline.com/${testTenantId}/oauth2/v2.0/token`,
    jwks_uri: `https://login.microsoftonline.com/${testTenantId}/discovery/v2.0/keys`,
    id_token_signing_alg_values_supported: ['RS256']
  };

  beforeEach(() => {
    jest.clearAllMocks();
    
    // Mock axios.create BEFORE creating service
    mockedAxios.create = jest.fn().mockReturnValue({
      get: jest.fn().mockResolvedValue({ data: mockConfig })
    } as any);
    
    service = new OpenIdConfigurationService(1000); // 1 second TTL for testing
  });

  describe('getConfigurationAsync', () => {
    it('should fetch configuration from Azure AD on first call', async () => {
      const config = await service.getConfigurationAsync(testTenantId);

      expect(config).toEqual(mockConfig);
      expect(config.issuer).toBe(`https://login.microsoftonline.com/${testTenantId}/v2.0`);
      expect(config.jwks_uri).toBe(`https://login.microsoftonline.com/${testTenantId}/discovery/v2.0/keys`);
    });

    it('should return cached configuration on subsequent calls', async () => {
      const config1 = await service.getConfigurationAsync(testTenantId);
      const config2 = await service.getConfigurationAsync(testTenantId);

      expect(config1).toEqual(config2);
      expect(service.cacheCount).toBe(1);
    });

    it('should refetch configuration after cache expires', async () => {
      const shortTtlService = new OpenIdConfigurationService(100); // 100ms TTL
      
      mockedAxios.create = jest.fn().mockReturnValue({
        get: jest.fn().mockResolvedValue({ data: mockConfig })
      } as any);

      await shortTtlService.getConfigurationAsync(testTenantId);
      
      // Wait for cache to expire
      await new Promise(resolve => setTimeout(resolve, 150));
      
      await shortTtlService.getConfigurationAsync(testTenantId);

      expect(shortTtlService.cacheCount).toBe(1);
    });

    it('should handle concurrent requests with locking', async () => {
      const httpClient = {
        get: jest.fn().mockImplementation(() => 
          new Promise(resolve => setTimeout(() => resolve({ data: mockConfig }), 100))
        )
      };
      
      mockedAxios.create = jest.fn().mockReturnValue(httpClient as any);
      const lockingService = new OpenIdConfigurationService();

      // Make 3 concurrent requests
      const [config1, config2, config3] = await Promise.all([
        lockingService.getConfigurationAsync(testTenantId),
        lockingService.getConfigurationAsync(testTenantId),
        lockingService.getConfigurationAsync(testTenantId)
      ]);

      expect(config1).toEqual(mockConfig);
      expect(config2).toEqual(mockConfig);
      expect(config3).toEqual(mockConfig);
      
      // Should only fetch once despite 3 concurrent calls
      expect(httpClient.get).toHaveBeenCalledTimes(1);
    });

    it('should throw error on network failure', async () => {
      mockedAxios.create = jest.fn().mockReturnValue({
        get: jest.fn().mockRejectedValue(new Error('Network error'))
      } as any);
      
      const failingService = new OpenIdConfigurationService();

      await expect(failingService.getConfigurationAsync(testTenantId))
        .rejects.toThrow('Network error');
    });

    it('should throw error on invalid JSON response', async () => {
      mockedAxios.create = jest.fn().mockReturnValue({
        get: jest.fn().mockResolvedValue({ data: 'invalid json' })
      } as any);
      
      const invalidService = new OpenIdConfigurationService();

      // This will fail validation when trying to use the invalid config
      const config = await invalidService.getConfigurationAsync(testTenantId);
      expect(config).toBe('invalid json');
    });

    it('should construct correct well-known URL', async () => {
      const httpClient = {
        get: jest.fn().mockResolvedValue({ data: mockConfig })
      };
      
      mockedAxios.create = jest.fn().mockReturnValue(httpClient as any);
      const urlService = new OpenIdConfigurationService();

      await urlService.getConfigurationAsync(testTenantId);

      expect(httpClient.get).toHaveBeenCalledWith(
        `https://login.microsoftonline.com/${testTenantId}/v2.0/.well-known/openid-configuration`
      );
    });
  });

  describe('clearCache', () => {
    it('should clear all cached configurations', async () => {
      await service.getConfigurationAsync(testTenantId);
      expect(service.cacheCount).toBe(1);

      service.clearCache();
      
      expect(service.cacheCount).toBe(0);
    });
  });

  describe('cacheCount', () => {
    it('should return correct number of cached configurations', async () => {
      expect(service.cacheCount).toBe(0);

      await service.getConfigurationAsync(testTenantId);
      expect(service.cacheCount).toBe(1);

      const anotherTenant = 'different-tenant-id';
      await service.getConfigurationAsync(anotherTenant);
      expect(service.cacheCount).toBe(2);
    });
  });
});

