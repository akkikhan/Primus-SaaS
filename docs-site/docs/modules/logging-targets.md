# Targets & Outputs

The Logging SDK supports multiple output targets. You can configure multiple targets simultaneously.

## Console Target

Writes logs to standard output (stdout).

- **Type**: `console`
- **Options**:
  - `pretty`: `true` for human-readable colors (dev), `false` for JSON (prod).

```json
{ "type": "console", "pretty": true }
```

## File Target

Writes logs to the file system.

- **Type**: `file`
- **Options**:
  - `path`: Path to the log file (absolute or relative).
  - `async`: `true` to enable non-blocking writes.
  - `maxFileSize`: Size in bytes before rotation.
  - `compress`: `true` to gzip rotated files.

```json
{ 
  "type": "file", 
  "path": "logs/app.log",
  "async": true,
  "maxFileSize": 10485760 
}
```

## Application Insights Target (.NET Only)

Directly sends logs to Azure Application Insights.

- **Type**: `applicationInsights`
- **Options**:
  - `connectionString`: Your Azure App Insights connection string.

```csharp
new TargetConfig 
{ 
    Type = "applicationInsights", 
    ConnectionString = "InstrumentationKey=..." 
}
```

## Custom Targets

You can implement your own targets by extending the base `Target` class (Node.js) or implementing `ITarget` (.NET).

This allows you to send logs to:
- Elasticsearch / ELK Stack
- Splunk
- Datadog
- Custom HTTP endpoints
