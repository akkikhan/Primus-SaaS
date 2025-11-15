import axios from 'axios';
import { JwksService } from '../../src/services/jwksService';
import { JwksCache } from '../../src/services/jwksCache';
import { JsonWebKeySet } from '../../src/types';

jest.mock('axios');
const mockedAxios = axios as jest.Mocked<typeof axios>;

describe('JwksService', () => {
  let service: JwksService;
  let cache: JwksCache;
  const testTenantId = 'cbd15a9b-cd52-4ccc-916a-00e2edb13043';
  const testJwksUri = `https://login.microsoftonline.com/${testTenantId}/discovery/v2.0/keys`;
  
  const mockJwks: JsonWebKeySet = {
    keys: [
      {
        kty: 'RSA',
        use: 'sig',
        kid: 'test-key-1',
        n: 'xGOr-H7A-PWRUm0lK2JvFnCZNvB7RkHzPUcqNZVmKO2JBh3Rj_7F1Nf3LQM8dO5JnrCU9D7qJVXdU6yqnJvMn7A',
        e: 'AQAB',
        x5t: 'dGVzdC1rZXktMQ',
        x5c: ['MIIDPjCCAiagAwIBAgIQVWmXY']
      },
      {
        kty: 'RSA',
        use: 'sig',
        kid: 'test-key-2',
        n: 'yHPs-I8B-QXSVn1mL3KwGoDaOwC8SlI0QVdraO3KCp4Rk_8G2Og4MRN9eP6KosCV-E8rKWYeV7zrnKvNo8Qo8B',
        e: 'AQAB',
        x5t: 'dGVzdC1rZXktMg',
        x5c: ['MIIDQjCCAiqgAwIBAgIRALxK']
      }
    ]
  };

  beforeEach(() => {
    jest.clearAllMocks();
    cache = new JwksCache();
    
    // Mock axios.create BEFORE creating service
    mockedAxios.create = jest.fn().mockReturnValue({
      get: jest.fn().mockResolvedValue({ data: mockJwks })
    } as any);
    
    service = new JwksService(cache);
  });

  describe('getJwksAsync', () => {
    it('should fetch JWKS from Azure AD on first call', async () => {
      const jwks = await service.getJwksAsync(testJwksUri);

      expect(jwks).toEqual(mockJwks);
      expect(jwks.keys).toHaveLength(2);
      expect(jwks.keys[0].kid).toBe('test-key-1');
    });

    it('should return cached JWKS on subsequent calls', async () => {
      const jwks1 = await service.getJwksAsync(testJwksUri);
      const jwks2 = await service.getJwksAsync(testJwksUri);

      expect(jwks1).toEqual(jwks2);
      expect(cache.count).toBe(1);
    });

    it('should handle concurrent requests with locking', async () => {
      const httpClient = {
        get: jest.fn().mockImplementation(() => 
          new Promise(resolve => setTimeout(() => resolve({ data: mockJwks }), 100))
        )
      };
      
      mockedAxios.create = jest.fn().mockReturnValue(httpClient as any);
      const lockingService = new JwksService(cache);

      // Make 3 concurrent requests
      const [jwks1, jwks2, jwks3] = await Promise.all([
        lockingService.getJwksAsync(testJwksUri),
        lockingService.getJwksAsync(testJwksUri),
        lockingService.getJwksAsync(testJwksUri)
      ]);

      expect(jwks1).toEqual(mockJwks);
      expect(jwks2).toEqual(mockJwks);
      expect(jwks3).toEqual(mockJwks);
      
      // Should only fetch once despite 3 concurrent calls
      expect(httpClient.get).toHaveBeenCalledTimes(1);
    });

    it('should throw error when JWKS has no keys', async () => {
      mockedAxios.create = jest.fn().mockReturnValue({
        get: jest.fn().mockResolvedValue({ data: { keys: [] } })
      } as any);
      
      const emptyService = new JwksService();

      await expect(emptyService.getJwksAsync(testJwksUri))
        .rejects.toThrow('No keys found in JWKS');
    });

    it('should throw error on network failure', async () => {
      mockedAxios.create = jest.fn().mockReturnValue({
        get: jest.fn().mockRejectedValue(new Error('Network error'))
      } as any);
      
      const failingService = new JwksService();

      await expect(failingService.getJwksAsync(testJwksUri))
        .rejects.toThrow('Network error');
    });

    it('should cache fetched JWKS', async () => {
      const jwks = await service.getJwksAsync(testJwksUri);

      expect(cache.count).toBe(1);
      expect(cache.get(testJwksUri)).toEqual(jwks);
    });
  });

  describe('getJwksForTenantAsync', () => {
    it('should fetch JWKS for specific tenant', async () => {
      const jwks = await service.getJwksForTenantAsync(testTenantId);

      expect(jwks).toEqual(mockJwks);
      expect(jwks.keys).toHaveLength(2);
    });

    it('should construct correct JWKS URI for tenant', async () => {
      const httpClient = {
        get: jest.fn().mockResolvedValue({ data: mockJwks })
      };
      
      mockedAxios.create = jest.fn().mockReturnValue(httpClient as any);
      const uriService = new JwksService();

      await uriService.getJwksForTenantAsync(testTenantId);

      expect(httpClient.get).toHaveBeenCalledWith(
        `https://login.microsoftonline.com/${testTenantId}/discovery/v2.0/keys`
      );
    });

    it('should cache JWKS by tenant', async () => {
      await service.getJwksForTenantAsync(testTenantId);

      expect(cache.count).toBe(1);
      expect(cache.get(testJwksUri)).toEqual(mockJwks);
    });
  });

  describe('clearCache', () => {
    it('should clear all cached JWKS', async () => {
      await service.getJwksAsync(testJwksUri);
      expect(cache.count).toBe(1);

      service.clearCache();
      
      expect(cache.count).toBe(0);
    });
  });

  describe('cacheCount', () => {
    it('should return correct number of cached JWKS', async () => {
      expect(service.cacheCount).toBe(0);

      await service.getJwksAsync(testJwksUri);
      expect(service.cacheCount).toBe(1);

      const anotherUri = 'https://different.com/jwks';
      await service.getJwksAsync(anotherUri);
      expect(service.cacheCount).toBe(2);
    });
  });
});

