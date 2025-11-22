import axios from 'axios';
import { JsonWebKeySet, JsonWebKey } from '../types';
import * as crypto from 'crypto';

export class JwksClient {
    private cache: Map<string, { keys: JsonWebKeySet; expiresAt: number }> = new Map();
    private readonly cacheTtl: number;

    constructor(cacheTtlHours: number = 24) {
        this.cacheTtl = cacheTtlHours * 60 * 60 * 1000;
    }

    async getSigningKey(jwksUrl: string, kid?: string): Promise<string> {
        const keys = await this.getJwks(jwksUrl);

        if (!keys || keys.keys.length === 0) {
            throw new Error('No keys found in JWKS');
        }

        const key = kid
            ? keys.keys.find(k => k.kid === kid)
            : keys.keys[0];

        if (!key) {
            throw new Error(`Key not found for kid: ${kid}`);
        }

        return this.convertJwkToPem(key);
    }

    private async getJwks(url: string): Promise<JsonWebKeySet> {
        const cached = this.cache.get(url);
        if (cached && cached.expiresAt > Date.now()) {
            return cached.keys;
        }

        try {
            const response = await axios.get<JsonWebKeySet>(url);
            const keys = response.data;

            this.cache.set(url, {
                keys,
                expiresAt: Date.now() + this.cacheTtl
            });

            return keys;
        } catch (error) {
            throw new Error(`Failed to fetch JWKS from ${url}: ${error instanceof Error ? error.message : String(error)}`);
        }
    }

    private convertJwkToPem(key: JsonWebKey): string {
        // If x5c is present, use the first certificate
        if (key.x5c && key.x5c.length > 0) {
            return `-----BEGIN CERTIFICATE-----\n${key.x5c[0]}\n-----END CERTIFICATE-----`;
        }

        // Otherwise construct RSA public key from n and e
        if (key.kty === 'RSA' && key.n && key.e) {
            return this.rsaPublicKeyToPem(key.n, key.e);
        }

        throw new Error('Unsupported key type or missing key data');
    }

    private rsaPublicKeyToPem(modulusB64: string, exponentB64: string): string {
        // This is a simplified PEM conversion. 
        // For production robustness, using 'crypto.createPublicKey' (Node 11.6+) is better if available.
        // Or 'node-jose' / 'jwk-to-pem'.
        // Since we want minimal deps, let's try crypto.createPublicKey if possible.

        try {
            const jwk = {
                kty: 'RSA',
                n: modulusB64,
                e: exponentB64
            };

            const keyObject = crypto.createPublicKey({
                key: jwk as any,
                format: 'jwk'
            });

            return keyObject.export({
                format: 'pem',
                type: 'spki'
            }) as string;
        } catch (e) {
            // Fallback or error
            throw new Error('Failed to convert JWK to PEM. Ensure Node.js version supports crypto.createPublicKey with JWK.');
        }
    }
}
