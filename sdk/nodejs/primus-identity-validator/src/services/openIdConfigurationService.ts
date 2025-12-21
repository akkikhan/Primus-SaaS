import axios, { AxiosInstance } from 'axios';
import { OpenIdConfiguration } from '../types';

interface CachedConfiguration {
  configuration: OpenIdConfiguration;
  cachedAt: Date;
  expiresAt: Date;
}

/**
 * Service for fetching and caching OpenID Connect configuration (Azure AD and generic OIDC)
 */
export class OpenIdConfigurationService {
  private readonly httpClient: AxiosInstance;
  private readonly configCache: Map<string, CachedConfiguration>;
  private readonly cacheTtl: number; // milliseconds
  private readonly fetchLock: Map<string, Promise<OpenIdConfiguration>>;

  constructor(cacheTtl: number = 24 * 60 * 60 * 1000) {
    this.httpClient = axios.create({ timeout: 10000 });
    this.configCache = new Map();
    this.cacheTtl = cacheTtl;
    this.fetchLock = new Map();
  }

  /**
   * Gets OpenID Connect configuration for a given authority or Azure AD tenant ID
   * @param authorityOrTenant Authority URL (https://...) or tenant ID (Azure AD)
   * @returns OpenID Connect configuration
   */
  async getConfigurationAsync(authorityOrTenant: string): Promise<OpenIdConfiguration> {
    const wellKnownUrl = this.getWellKnownUrl(authorityOrTenant);
    
    // Check cache
    const cached = this.configCache.get(wellKnownUrl);
    if (cached && Date.now() < cached.expiresAt.getTime()) {
      return cached.configuration;
    }
    
    // Check if fetch is in progress
    const inProgress = this.fetchLock.get(wellKnownUrl);
    if (inProgress) {
      return inProgress;
    }
    
    // Fetch from Azure AD
    const fetchPromise = this.fetchConfiguration(wellKnownUrl);
    this.fetchLock.set(wellKnownUrl, fetchPromise);
    
    try {
      const config = await fetchPromise;
      
      // Cache the configuration
      this.configCache.set(wellKnownUrl, {
        configuration: config,
        cachedAt: new Date(),
        expiresAt: new Date(Date.now() + this.cacheTtl)
      });
      
      return config;
    } finally {
      this.fetchLock.delete(wellKnownUrl);
    }
  }

  /**
   * Fetches OpenID Connect configuration from Azure AD
   * @param url Well-known configuration URL
   * @returns OpenID Connect configuration
   */
  private async fetchConfiguration(url: string): Promise<OpenIdConfiguration> {
    const response = await this.httpClient.get<OpenIdConfiguration>(url);
    return response.data;
  }

  /**
   * Gets the well-known OpenID Connect configuration URL
   * @param authorityOrTenant Authority URL or Azure AD tenant ID
   * @returns Well-known configuration URL
   */
  private getWellKnownUrl(authorityOrTenant: string): string {
    // If caller passed a bare tenant ID (no scheme), assume Azure AD
    const isBareTenantId = !authorityOrTenant.includes('://');
    const authority = isBareTenantId
      ? `https://login.microsoftonline.com/${authorityOrTenant}`
      : authorityOrTenant;

    const url = new URL(authority);
    const isAzureHost =
      url.hostname.toLowerCase().includes('login.microsoftonline.com') ||
      url.hostname.toLowerCase().includes('sts.windows.net');

    // Append /v2.0 for Azure AD when missing; leave others untouched
    let path = url.pathname.endsWith('/') ? url.pathname.slice(0, -1) : url.pathname;
    if (isAzureHost && !path.toLowerCase().endsWith('/v2.0')) {
      path = `${path}/v2.0`;
    }

    // Build well-known path
    url.pathname = `${path}/.well-known/openid-configuration`;
    url.search = '';

    return url.toString();
  }

  /**
   * Clears the configuration cache
   */
  clearCache(): void {
    this.configCache.clear();
  }

  /**
   * Gets the number of cached configurations
   */
  get cacheCount(): number {
    return this.configCache.size;
  }
}
