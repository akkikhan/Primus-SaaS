# 🎬 Enterprise Demo Script

**Audience:** Engineering Leadership (CTO, Lead Architect)
**Context:** Client uses .NET Backend + Angular Frontend.
**Goal:** Prove that `PrimusSaaS.Identity.Validator` is the *only* logical choice for their Portal.

---

## 🛠️ Setup (The "Mise en place")

1.  **VS Code:** Open `Program.cs` in a .NET 8 Web API project.
2.  **Browser:** Open a "Mock Dashboard" (or just a Swagger UI).
3.  **Terminal:** Ready to run `dotnet add package`.

---

## 🎥 The Script

### 1. The Hook: "Enterprise Identity is Hard"
**You:** "We know you sell to large enterprises. That means your Portal needs to support Azure AD, Multi-Tenancy, and strict security compliance."
**You:** "Building this from scratch in .NET takes weeks. Maintaining it takes forever. Let me show you how to do it in 3 minutes."

### 2. The Backend (The "Native" Feel)
*Action: Switch to VS Code `Program.cs`.*

**You:** "This is a standard .NET 8 API. To secure it, we don't ask you to run a sidecar or call our API. You just add our NuGet package."

*Action: Type/Paste:*
```csharp
builder.Services.AddPrimusIdentity(options => {
    options.Issuers = new() {
        new IssuerConfig {
            Name = "AzureAD",
            Type = IssuerType.Oidc,
            // We automatically handle the JWKS caching and validation
            Authority = "https://login.microsoftonline.com/common/v2.0",
            Audiences = new[] { "api://your-portal-id" }
        }
    };
});
```

**You:** "That's it. We now have a multi-tenant, caching, validating middleware running **inside** your process."

### 3. The Frontend (The Angular Bridge)
*Action: Open `ANGULAR_INTEGRATION.md`.*

**You:** "I know your frontend is Angular. We've built a drop-in `HttpInterceptor` for you."
**You:** "Your frontend team doesn't need to learn OIDC protocols. They just register this interceptor, and every API call is automatically secured with the correct Azure AD token."

### 4. The "Zero Dependency" Drop
**You:** "Here is the most important part for a governance platform."
**You:** "This code runs 100% on your servers. It validates tokens directly against Azure AD."
**You:** "If *our* website goes down, *your* customers can still log in. We are not a runtime dependency. We are a library."

### 5. The Close
**You:** "So, you get Enterprise SSO, Angular compatibility, and Zero Runtime Risk. Ready to try it?"

---

## 📝 Objections Handling

**Q: "We use IdentityServer."**
**A:** "Great! We support any OIDC provider. You can point our validator at your IdentityServer authority URL, and it works exactly the same way. We just make the validation easier."

**Q: "We need to map Azure Groups to Application Roles."**
**A:** "We have a `TenantResolver` callback. You can write 5 lines of C# to map `groups` claim to your internal roles right in the middleware."

**Q: "What about performance?"**
**A:** "We cache the Azure AD signing keys for 24 hours (configurable). Validation is purely local cryptography. It adds < 1ms to your request latency."
