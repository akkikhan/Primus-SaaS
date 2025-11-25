# Verification Guide - @primus-saas/logging (Node.js)

This mirrors the NuGet verification steps.

## 1) Console + file smoke test
```bash
npm install
npm run build
npm test
```
- Expect console output (JSON) for INFO+ logs.
- Check `logs/app.log` (configure a file target) for appended entries.

## 2) Middleware flow
- Add `primusLoggingMiddleware(logger)` before routes.
- Hit a route and confirm logs include `requestId`, `method`, `path`, `statusCode`, and duration.

## 3) PII masking
- Log `{ email: 'a@b.com', password: 'secret' }` with masking enabled.
- Verify the stored values are masked/redacted.

## 4) Rotation
- Configure `maxFileSize: 200` and write several logs.
- Expect `app.log.1` (or `.1.gz` if compression enabled) to appear.

## 5) Application Insights (optional)
- Configure the connection string.
- Confirm traces arrive with levels mapped to SeverityLevel in Azure.
