import 'express-serve-static-core';
import type { PrimusUser } from 'primus-identity-validator';

declare global {
  namespace Express {
    interface Request {
      primusUser?: PrimusUser;
    }
  }
}

export {};
