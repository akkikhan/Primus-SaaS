# Token Refresh Guide (Primus Identity Validator)

Primus Identity Validator now exposes a pluggable refresh interface. The built-in in-memory implementation is **for development/demo only**. For production, implement `ITokenRefreshService` backed by durable storage (database, cache) with rotation and revocation.

## Quick Start (Development)
```csharp
builder.Services.AddPrimusIdentity(options =>
{
    options.Issuers = /* ... */;
    options.TokenRefresh = new TokenRefreshOptions
    {
        Enabled = true,
        UseInMemoryStore = true,
        AccessTokenTtl = TimeSpan.FromMinutes(30),
        RefreshTokenTtl = TimeSpan.FromDays(30)
    };
});
```
This registers `InMemoryTokenRefreshService` and a no-op service when disabled.

## Production Guidance
- Implement `ITokenRefreshService` using durable storage (e.g., Redis/SQL) with:
  - Token rotation on each refresh
  - Revocation support (e.g., logout, compromise)
  - Short-lived access tokens; longer-lived refresh tokens
  - Anti-replay (rotate and invalidate previous refresh token)
- Register your implementation:
```csharp
builder.Services.AddSingleton<ITokenRefreshService, YourDurableRefreshService>();
```

## API Contract
- `IssueRefreshTokenAsync(userId)` → returns a new refresh token for a user identifier.
- `ValidateRefreshTokenAsync(token)` → returns true if valid and not expired/revoked.
- `RefreshAsync(token)` → returns a new access token (and rotated refresh token). Return `TokenRefreshResult.Failed(reason)` on failure.

## Defaults and Safety
- Disabled by default.
- In-memory store is non-durable and not for production.
- Access token and refresh token TTLs must be positive; validation enforces this.
