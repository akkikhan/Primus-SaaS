import jwt from 'jsonwebtoken';
import { JwtPayload } from './types';

export interface SignLocalTokenOptions {
  userId: string;
  email: string;
  name?: string;
  roles?: string[];
  issuer: string;
  audience: string;
  secret: string;
  expiresInSeconds?: number;
  additionalClaims?: Record<string, unknown>;
}

export function signLocalToken(options: SignLocalTokenOptions): string {
  const {
    userId,
    email,
    name,
    roles = [],
    issuer,
    audience,
    secret,
    expiresInSeconds = 3600,
    additionalClaims = {}
  } = options;

  if (!secret) {
    throw new Error('signLocalToken: secret is required and must match the configured Local issuer.');
  }

  if (!issuer || !audience) {
    throw new Error('signLocalToken: issuer and audience are required.');
  }

  const payload: JwtPayload = {
    sub: userId,
    email,
    name: name ?? email,
    role: roles,
    iss: issuer,
    aud: audience,
    ...additionalClaims
  };

  return jwt.sign(payload, secret, {
    algorithm: 'HS256',
    expiresIn: expiresInSeconds,
    issuer,
    audience
  });
}
