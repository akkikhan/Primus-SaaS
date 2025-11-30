# Senior Management Live Demo Script (Revised)

**Goal**: Demonstrate how quickly and securely we can integrate **Identity, Logging, and Notifications** into a new application using Primus SaaS packages.

**Time Estimate**: 5-7 Minutes

---

## 1. Introduction (1 Minute)
"Good morning/afternoon. Today, I want to show you the power of our new Identity Validator module. 

Usually, integrating Azure AD and Auth0 takes days of configuration and complex code. With Primus, we can do it in minutes, ensuring enterprise-grade security out of the box.

I'm going to build a secure API right now, live, to prove it."

---

## 2. The Setup (1 Minute)
*Open VS Code to the `LiveDemoApi` folder. Show `Program.cs`.*

"I have a standard .NET 8 Web API here. It has no security. Anyone can access it.

I've already installed our packages: `PrimusSaaS.Logging`, `PrimusSaaS.Identity.Validator`, and `PrimusSaaS.Notifications`.
But right now, they are all disabled. The app is insecure and has no logging."

---

## 3. The Integration (2 Minutes)
*Open `Program.cs` and highlight the 4 key steps.*

"To secure this app and add enterprise features, I only need to uncomment a few lines of code.

### Step 1: Logging (1 Minute)
"First, let's turn on the lights. I'll enable Primus Logging."
*Uncomment `LIVE DEMO STEP 1` in `Program.cs` (Service Registration & Middleware).*

```csharp
// 1. Register Service
builder.Logging.AddPrimus(options => ... );

// 2. Add Middleware
app.UsePrimusLogging();
```

### Step 2: Identity (2 Minutes)
"Now, let's secure the door. I'll enable Primus Identity."
*Uncomment `LIVE DEMO STEP 2` in `Program.cs`.*

**Registration:**
```csharp
builder.Services.AddPrimusIdentity(options => ... );
```

**Middleware:**
```csharp
app.UseAuthentication();
app.UseAuthorization();
```

**Secure Endpoint:**
*Uncomment `.RequireAuthorization()` on `/weatherforecast`.*

### Step 3: Notifications (2 Minutes)
"Finally, let's talk to our users. I'll enable Primus Notifications."
*Uncomment `LIVE DEMO STEP 3` in `Program.cs`.*

**Registration:**
```csharp
builder.Services.AddPrimusNotifications(notifications => ... );
```

**Endpoints:**
*Uncomment the `/notifications/*` endpoints.*"



---

## 4. The Proof (2 Minutes)
*Open the Integrated Terminal.*

"Now, let's run it."
```bash
dotnet run
```

1.  **Open Browser**: Go to the URL shown (e.g., `http://localhost:5221/swagger`).
2.  **Try to Execute**: Click "Try it out" on `/weatherforecast`.
3.  **Show Error**: Show the **401 Undocumented** or **401 Unauthorized** response.
    "As you can see, access is denied. The API is secure."

4.  **Diagnostics (Optional)**:
    "If we want to see what's happening under the hood, I can check my diagnostics endpoint."
    *(Navigate to `/primus/diagnostics`)*
    "This confirms that both Azure AD and Auth0 are loaded and ready to validate tokens."

---

## 5. Conclusion (1 Minute)
"In less than 5 minutes, we went from zero security to a multi-tenant, enterprise-secure application. 

This standardization means:
1.  **Speed**: Developers focus on business logic, not auth plumbing.
2.  **Security**: We control the standards centrally.
3.  **Flexibility**: We can swap providers without rewriting code.

Any questions?"
