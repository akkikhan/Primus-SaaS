import { PrimusUser } from 'primus-identity-validator';
declare global {
    namespace Express {
        interface Request {
            primusUser?: PrimusUser;
        }
    }
}
//# sourceMappingURL=index.d.ts.map