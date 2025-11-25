# Troubleshooting - @primus-saas/logging (Node.js)

## No logs appearing
- Ensure `minLevel` allows the log (INFO by default).
- If buffering is enabled, reduce `bufferSize` or `flushIntervalMs` during testing.
- For file target, verify the path is writable and directories exist.

## Rotation not happening
- Set `maxFileSize` (bytes) and `maxRetainedFiles`.
- Remember rotation occurs on the next write after the threshold is reached.

## Application Insights not receiving data
- Check `APPINSIGHTS_CONNECTION_STRING` and outbound HTTPS.
- Severity is mapped from Primus log levels; confirm the role name in Azure for filtering.

## PII still visible
- Confirm `masking.enabled = true`.
- Add custom keys to `customSensitiveKeys` (e.g., `authorization`, `apiKey`).
- Strategy `hash`/`partial` can help when you need debugging while hiding full values.

## Middleware issues
- Ensure `app.use(primusLoggingMiddleware(logger))` is registered before routes.
- If user/tenant missing, confirm authentication middleware sets `req.primusUser` / `req.primusTenantContext`.

## Performance tips
- Enable buffering in high-throughput scenarios.
- Use file rotation with compression to cap disk usage.
