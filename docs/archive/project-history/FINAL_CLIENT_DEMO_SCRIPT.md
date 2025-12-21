# Final Client Demo Script: Primus SaaS Packages

This script guides you through a live demonstration of the Primus SaaS packages (`Identity Validator`, `Notifications`, `Logging`), highlighting the "Package vs. Traditional" approach.

## 1. Preparation

1.  **Open VS Code** to `c:\Users\Akki\Primus SaaS`.
2.  **Open Terminal 1**: Run the Backend.
    ```powershell
    cd examples/LiveDemoApi
    dotnet run
    ```
3.  **Open Terminal 2**: Run the Frontend.
    ```powershell
    cd examples/LiveDemoFrontend
    npm run dev
    ```
4.  **Open Browser**: Navigate to `http://localhost:5173`.
5.  **Login**: Use the "Local JWT" option (pre-filled) to get into the dashboard.
6.  **Verify State**: Ensure "System Health" shows **Identity: Enabled** and **Notifications: Ready**.

---

## 2. Introduction (The "Why")

*   **Speaker**: "Today I'm going to show you the power of the Primus SaaS packages. We often spend days wiring up boilerplate for Identity, Logging, and Notifications. With Primus, it's one line of code."
*   **Action**: Show the running dashboard. Point out the "System Health" card.
*   **Speaker**: "This dashboard is live. It's monitoring our backend modules. Let's see what happens when we disable them."

---

## 3. Demo: Identity Validator

### Step A: The "One Line" Power
1.  **Go to VS Code**: Open `examples/LiveDemoApi/Program.cs`.
2.  **Locate Line ~68**:
    ```csharp
    builder.Services.AddPrimusIdentity(opts => builder.Configuration.GetSection("PrimusIdentity").Bind(opts));
    ```
3.  **Speaker**: "Here is our entire Identity setup. Multi-issuer validation, security headers, everything. Just one line."

### Step B: Break the App
1.  **Action**: **Comment out** that line.
    ```csharp
    // builder.Services.AddPrimusIdentity(opts => builder.Configuration.GetSection("PrimusIdentity").Bind(opts));
    ```
2.  **Action**: Save the file. (If not using hot reload, restart the backend in Terminal 1).
3.  **Go to Browser**: Refresh the page or wait for the auto-refresh.
4.  **Observation**:
    *   The **System Health** card for "Identity" should turn **RED** or show "Disabled".
    *   **Message**: "Primus Identity Validator is commented out or not registered in Program.cs."
5.  **Speaker**: "The application immediately detects the missing security layer. In a traditional app, you might just fail silently or crash. Here, we know exactly what's wrong."

### Step C: The "Traditional" Nightmare
1.  **Go to VS Code**: Look at the commented-out code **immediately above** the Primus line.
2.  **Speaker**: "To replace that one line, I would have to write all this..."
3.  **Action**: Highlight the block under `// Question: Traditional Method`.
    ```csharp
    // Question: Traditional Method (Complete Implementation)
    // builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    // ...
    ```
4.  **Speaker**: "And this only covers one provider! Primus handles Auth0, Azure AD, and Local JWTs automatically."

### Step D: Restore
1.  **Action**: Uncomment the `AddPrimusIdentity` line. Save.
2.  **Browser**: Verify "System Health" returns to **Green/Enabled**.

---

## 4. Demo: Notifications

### Step A: The "One Line" Power
1.  **Go to VS Code**: Locate the `AddPrimusNotifications` block.
2.  **Action**: Point to the `// Question: Traditional Method` block **above** it.
    ```csharp
    // Question: Traditional Method (Complete Implementation)
    // builder.Services.AddTransient<System.Net.Mail.SmtpClient>(sp => ...
    ```
3.  **Speaker**: "Same for notifications. Traditionally, you'd manually register SmtpClient, handle retries, and build your own template engine. Primus gives you SMTP, Twilio, and File Templates out of the box."

### Step B: Break the App
1.  **Action**: **Comment out** the entire `AddPrimusNotifications` block (lines ~101-154).
    *   *Tip: You can just comment out the starting `builder.Services.AddPrimusNotifications` line and the closing brace/semicolon if you want to be quick, or use block comment `/* ... */`.*
2.  **Action**: Save and Restart Backend (Required as services change).
3.  **Go to Browser**: Navigate to the **Notifications** tab.
4.  **Action**: Try to send a "Welcome Email".
5.  **Observation**:
    *   An error popup/toast will appear.
    *   **Message**: "Unable to resolve service for type 'PrimusSaaS.Notifications.Abstractions.INotificationService'..."
6.  **Speaker**: "The app crashes when trying to send. I would need to manually implement `SmtpClient`, retry logic, and template parsing. Primus gave me all that for free."

### Step C: Restore
1.  **Action**: Uncomment the block. Save and Restart Backend.
2.  **Action**: Send the email again. Show "Success".

---

## 5. Demo: Logging

### Step A: The "One Line" Power
1.  **Go to VS Code**: Locate the `AddPrimus` logging line.
2.  **Action**: Point to the `// Question: Traditional Method` block **above** it.
    ```csharp
    // Question: Traditional Method (Complete Implementation)
    // builder.Logging.ClearProviders();
    // ...
    ```
3.  **Speaker**: "Structured logging with PII redaction and multiple sinks (File, Console, AI). Doing this manually requires configuring Serilog or NLog and writing custom formatters."

### Step B: Break the App
1.  **Action**: **Comment out** that line.
2.  **Action**: Save and Restart Backend.
3.  **Go to Browser**: Navigate to the **Logs** tab.
4.  **Action**: Click "Generate Test Log".
5.  **Observation**:
    *   The log viewer (tail) will **stop updating** or show "Log file not found" (if you deleted it).
    *   The structured data is gone.
6.  **Speaker**: "We lost all our rich telemetry. No more structured logs, no more PII protection."

### Step C: Restore
1.  **Action**: Uncomment. Save and Restart.
2.  **Action**: Click "Generate Test Log". See the logs flow in again.

---

## 6. Conclusion

*   **Speaker**: "Primus SaaS packages reduce hundreds of lines of boilerplate to single lines of configuration, while adding robustness, diagnostics, and best practices by default. This allows us to focus on business logic, not plumbing."
