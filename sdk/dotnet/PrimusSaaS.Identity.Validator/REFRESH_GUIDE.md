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
This registers an in-memory refresh service (dev-only) and a no-op service when disabled.

## Production Guidance
- Use a durable store (`UseDurableStore = true`) and register an `IRefreshTokenStore` (e.g., Redis/SQL):
```csharp
builder.Services.AddSingleton<IRefreshTokenStore, DistributedRefreshTokenStore>(); // requires IDistributedCache (Redis/SQL)
builder.Services.Configure<TokenRefreshOptions>(opt =>
{
    opt.Enabled = true;
    opt.UseDurableStore = true;
    opt.AccessTokenTtl = TimeSpan.FromMinutes(15);
    opt.RefreshTokenTtl = TimeSpan.FromDays(30);
});
```
- Durable store recommendations:
  - Token rotation on each refresh
  - Revocation support (e.g., logout, compromise)
  - Short-lived access tokens; longer-lived refresh tokens
  - Anti-replay (rotate and invalidate previous refresh token)

## API Contract
- `IssueRefreshTokenAsync(userId)` → returns a new refresh token for a user identifier.
- `ValidateRefreshTokenAsync(token)` → returns true if valid and not expired/revoked.
- `RefreshAsync(token)` → returns a new access token (and rotated refresh token). Return `TokenRefreshResult.Failed(reason)` on failure.

## Defaults and Safety
- Disabled by default.
- In-memory store is non-durable and not for production.
- Access token and refresh token TTLs must be positive; validation enforces this.
