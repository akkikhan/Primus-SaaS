# Policy Helpers Scope

Authorization policy helpers are not bundled here to avoid coupling to specific claim shapes. Use ASP.NET Core authorization directly:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAudience", policy =>
        policy.RequireClaim("aud", "api://your-api"));
});
```

Rationale:
- Claim naming varies by IdP; helpers without a fixed mapping would be brittle.
- Keeps this library focused on validation/diagnostics; policies stay in the app.
```
