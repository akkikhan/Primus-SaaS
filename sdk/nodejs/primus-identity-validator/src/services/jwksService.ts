import axios, { AxiosInstance } from 'axios';
import { JsonWebKeySet } from '../types';
import { JwksCache } from './jwksCache';

/**
 * Service for fetching and caching JSON Web Key Sets from Azure AD
 */
export class JwksService {
  private readonly httpClient: AxiosInstance;
  private readonly cache: JwksCache;
  private readonly fetchLock: Map<string, Promise<JsonWebKeySet>>;

  constructor(cache?: JwksCache) {
    this.httpClient = axios.create({ timeout: 10000 });
    this.cache = cache ?? new JwksCache();
    this.fetchLock = new Map();
  }

  /**
   * Gets JWKS from the specified URI with caching
   * @param jwksUri JWKS URI from OpenID configuration
   * @returns JSON Web Key Set
   */
  async getJwksAsync(jwksUri: string): Promise<JsonWebKeySet> {
    // Try cache first
    const cached = this.cache.get(jwksUri);
    if (cached) {
      return cached;
    }

    // Check if fetch is in progress
    const inProgress = this.fetchLock.get(jwksUri);
    if (inProgress) {
      return inProgress;
    }

    // Fetch from Azure AD
    const fetchPromise = this.fetchJwks(jwksUri);
    this.fetchLock.set(jwksUri, fetchPromise);

    try {
      const keySet = await fetchPromise;
      
      if (!keySet.keys || keySet.keys.length === 0) {
        throw new Error('No keys found in JWKS');
      }

      // Cache the key set
      this.cache.set(jwksUri, keySet);

      return keySet;
    } finally {
      this.fetchLock.delete(jwksUri);
    }
  }

  /**
   * Gets JWKS for a specific Azure AD tenant
   * @param tenantId Azure AD tenant ID
   * @returns JSON Web Key Set
   */
  async getJwksForTenantAsync(tenantId: string): Promise<JsonWebKeySet> {
    const jwksUri = `https://login.microsoftonline.com/${tenantId}/discovery/v2.0/keys`;
    return this.getJwksAsync(jwksUri);
  }

  /**
   * Fetches JWKS from the specified URI
   * @param uri JWKS URI
   * @returns JSON Web Key Set
   */
  private async fetchJwks(uri: string): Promise<JsonWebKeySet> {
    const response = await this.httpClient.get<JsonWebKeySet>(uri);
    return response.data;
  }

  /**
   * Clears the JWKS cache
   */
  clearCache(): void {
    this.cache.clear();
  }

  /**
   * Gets the number of cached JWKS
   */
  get cacheCount(): number {
    return this.cache.count;
  }
}
