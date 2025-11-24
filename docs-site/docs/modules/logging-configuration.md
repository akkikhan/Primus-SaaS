# Logging SDK Configuration

This guide covers configuration options for the Primus Logging SDKs (.NET and Node.js).

## .NET Configuration

Configure in `Program.cs` using `AddPrimus`:

```csharp
builder.Logging.AddPrimus(options =>
{
    options.ApplicationId = "MY-APP";
    options.Environment = "production";
    
    // Targets
    options.Targets = new List<TargetConfig>
    {
        new() { Type = "console", Pretty = true },
        new() { Type = "file", Path = "logs/app.log" }
    };
    
    // PII Masking
    options.Pii.MaskEmails = true;
    options.Pii.MaskCreditCards = true;
    options.Pii.CustomPatterns = new Dictionary<string, string>
    {
        { @"\bSSN\b", "***-**-****" }
    };
});
```

### Options Reference (.NET)

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `ApplicationId` | `string` | `null` | Identifier for your application. |
| `Environment` | `string` | `Development` | Current environment (Production, Staging). |
| `Targets` | `List<TargetConfig>` | `[]` | List of output targets. |
| `Pii` | `PiiOptions` | `Defaults` | PII masking configuration. |

---

## Node.js Configuration

Configure via the `Logger` constructor:

```javascript
const logger = new Logger({
  applicationId: 'MY-APP',
  environment: 'production',
  minLevel: 'info',
  targets: [
    { type: 'console', pretty: true },
    { type: 'file', path: 'logs/app.log' }
  ],
  pii: {
    maskEmails: true,
    maskCreditCards: true
  }
});
```

### Options Reference (Node.js)

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `applicationId` | `string` | `null` | Identifier for your application. |
| `environment` | `string` | `development` | Current environment. |
| `minLevel` | `string` | `'info'` | Minimum log level to record. |
| `targets` | `Array` | `[]` | List of output targets. |
| `pii` | `Object` | `Defaults` | PII masking configuration. |
