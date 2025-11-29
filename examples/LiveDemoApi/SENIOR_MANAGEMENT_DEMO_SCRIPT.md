# Senior Management Live Demo Script (Revised)

**Goal**: Demonstrate how quickly and securely we can integrate multiple identity providers (Azure AD and Auth0) into a new application using `PrimusSaaS.Identity.Validator`.

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

I've already installed our package: `PrimusSaaS.Identity.Validator`."

---

## 3. The Integration (2 Minutes)
*Open `Program.cs` and highlight the 4 key steps.*

"To secure this app, I only need to add 4 things:"

**Step 1: Registration**
"First, I register the Primus Identity service. This one line reads all our configuration."
```csharp
builder.Services.AddPrimusIdentity(options =>
{
    builder.Configuration.GetSection("PrimusIdentity").Bind(options);
});
```

**Step 2: Middleware**
"Next, I add the security gates. Authentication checks who you are; Authorization checks what you can do."
```csharp
app.UseAuthentication();
app.UseAuthorization();
```

**Step 3: Configuration**
*Switch to `appsettings.json`.*
"This is where the magic happens. I don't write code for Azure or Auth0. I just define them here."
*(Show the JSON structure)*
"I can add as many providers as I want—Okta, Cognito, Google—just by adding to this list.
*Note: I have pre-filled this with our demo tenant IDs so we don't have to type them out live.*"

**Step 4: Securing an Endpoint**
*Back to `Program.cs`.*
"Finally, I lock the door. I add `.RequireAuthorization()` to my weather forecast."

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
