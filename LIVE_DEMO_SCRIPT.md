# 🎬 Primus SaaS - The "Magic" Live Demo Script

**Objective:** Go from a blank, insecure application to a fully secure, enterprise-grade app in < 5 minutes live.

---

## 🎭 Act 1: The Setup (Do this BEFORE the meeting)

1.  **Start the Primus Platform:**
    *   Open Terminal 1: `cd "C:\Users\Akki\Primus SaaS\portal\backend" && dotnet run`
    *   Open Terminal 2: `cd "C:\Users\Akki\Primus SaaS\portal\frontend" && npm run dev`
    *   **Verify:** Go to `http://localhost:5173` (or whatever port frontend uses) and login as Admin.

2.  **Prepare the "Customer" App:**
    *   Create a folder: `C:\Demo\MySecureApp`
    *   Open Terminal there: `dotnet new webapi`
    *   **CRITICAL:** Configure Local NuGet Source (so you can "install" the package live):
        ```powershell
        dotnet nuget add source "C:\Users\Akki\Primus SaaS\nupkg" -n LocalPrimus
        ```
        *(If it says it already exists, that's fine).*

3.  **Open VS Code:**
    *   Open the `C:\Demo\MySecureApp` folder.
    *   Open `Program.cs`.
    *   Run it once (`dotnet run`) to make sure it works.
    *   **Keep it open.**

---

## 🎭 Act 2: The "Before" State (Start of Meeting)

**🗣️ You Say:**
> "I'm going to show you how a developer would normally build a secure app, but using Primus. Here is a standard, brand-new .NET application. It has **zero security**. Anyone can access it."

**👉 Action:**
1.  Run the app: `dotnet run`
2.  Open Browser: `http://localhost:5xxx/weatherforecast`
3.  **Result:** You see JSON data.
4.  **Point out:** "See? No login required. Totally unsafe."
5.  Stop the app (Ctrl+C).

---

## 🎭 Act 3: The Integration (The "Magic" Part)

**🗣️ You Say:**
> "Now, let's secure this. Usually, this takes 2 weeks. Watch this."

### Step 1: The Portal
**👉 Action:**
1.  Go to Primus Portal (already open).
2.  Click **"Applications"** -> **"New Application"**.
3.  Name: `Live Demo App`.
4.  Click **Create**.
5.  **✨ THE MOMENT:**
    *   **Old Way:** Copy Secret from screen.
    *   **New Way:** "I just got an email with my credentials and integration guide."
    *   *Check your email (or fake it) and get the Secret.*
    *   *Paste them into a temporary Notepad so you have them.*

### Step 2: The Install
**👉 Action:**
1.  Go back to VS Code.
2.  Run command:
    ```powershell
    dotnet add package PrimusSaaS.Identity.Validator
    ```
3.  **Point out:** "I'm just installing the Primus SDK. No custom code yet."

### Step 3: The Code (5 Lines)
**👉 Action:**
1.  Open `Program.cs`.
2.  **Add using statement:**
    ```csharp
    using PrimusSaaS.Identity.Validator;
    ```
3.  **Add the Service (Before `builder.Build()`):**
    ```csharp
    builder.Services.AddPrimusIdentity(options => {
        options.Issuers = new[] {
            new IssuerConfig {
                Name = "Local",
                Type = IssuerType.Jwt,
                Issuer = "https://primus-portal.com", // Matches backend generation
                Secret = "YOUR_CLIENT_SECRET_HERE", // Paste from Notepad
                Audiences = new[] { "YOUR_CLIENT_ID_HERE" } // Paste from Notepad
            }
        };
    });
    ```
4.  **Add the Middleware (Before `app.MapControllers()`):**
    ```csharp
    app.UseAuthentication(); // Standard .NET
    app.UseAuthorization();  // Standard .NET
    ```
5.  **Lock the Door:**
    *   Open `Controllers/WeatherForecastController.cs`.
    *   Add `[Authorize]` above the class name.

---

## 🎭 Act 4: The Payoff (The "Wow")

**🗣️ You Say:**
> "That's it. We are done. Let's see if it works."

**👉 Action:**
1.  Run the app: `dotnet run`
2.  **Test 1 (Fail):** Refresh the browser (`/weatherforecast`).
    *   **Result:** `401 Unauthorized` (or blank screen depending on browser).
    *   **Say:** "Boom. The door is locked. No one gets in without a key."

3.  **Test 2 (Success):**
    *   Go to Portal -> Applications -> Live Demo App.
    *   Click **"Generate Test Token"**.
    *   Use a tool like **Postman** (or a simple `curl` command if you're fast).
    *   Request: `GET /weatherforecast` with `Authorization: Bearer <TOKEN>`.
    *   **Result:** `200 OK` with Data.
    *   **Say:** "And with the valid key, we are in. We just saved 2 weeks of work in 3 minutes."

---

## 💡 Pro Tips for "Impressing"

1.  **Don't Type Everything:** Have the code snippet (Step 3) ready in a "Cheat Sheet" file. Copy-paste it. It shows *ease*, not typing speed.
2.  **Show the Logs (Bonus):** If you have time, add the Logging package (`dotnet add package PrimusSaaS.Logging`) and show the logs appearing in the console with colors.
3.  **Mention "Enterprise":** While doing this, say words like *"This is fully compliant with SOC2 audit requirements"* and *"This handles key rotation automatically"*.
