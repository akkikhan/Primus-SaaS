# Primus SaaS Live Demo Walkthrough

This guide provides a step-by-step script for presenting the Primus SaaS packages to senior management. The goal is to demonstrate the value of the packages by showing the "Primus Way" (clean, working) versus the "Traditional/Broken" way (commenting out the code).

## Prerequisites

1.  **Backend Running**: Ensure `LiveDemoApi` is running (Port 5221).
2.  **Frontend Running**: Ensure `LiveDemoFrontend` is running (Port 5173).
3.  **Browser**: Open `http://localhost:5173`.
4.  **Editor**: Open `c:\Users\Akki\Primus SaaS\examples\LiveDemoApi\Program.cs` in VS Code.

---

## Demo Flow

### 1. Introduction (The "Primus Way")

*   **Action**: Show the `LiveDemoFrontend` Dashboard.
*   **Script**: "This is our standard application. It uses Primus SaaS packages for Identity, Logging, and Notifications. Everything is working smoothly."
*   **Demonstrate**:
    *   **Login**: Log in using 'Local JWT' (or Auth0 if configured).
    *   **Dashboard**: Click "Fetch Secure Data". Show the green "Access Granted" card.
    *   **Logs**: Go to the "Logs" tab. Show the structured logs appearing.
    *   **Code**: Switch to VS Code (`Program.cs`).
    *   **Script**: "Look at our `Program.cs`. We've replaced hundreds of lines of boilerplate with just three key lines:"
        *   **Line 46**: `builder.Logging.AddPrimus(...)`
        *   **Line 68**: `builder.Services.AddPrimusIdentity(...)`
        *   **Line 101**: `builder.Services.AddPrimusNotifications(...)`

---

### 2. Identity Validator Demo

*   **Script**: "Let's see what happens if we remove the Primus Identity Validator. Traditionally, you'd need complex JWT bearer configuration here."
*   **Action (VS Code)**:
    *   Go to **Line 68**.
    *   **Comment out**: `// builder.Services.AddPrimusIdentity(...)`
    *   **Save** the file (Wait for hot reload).
*   **Action (Browser)**:
    *   Go to the **Dashboard** tab.
    *   Click **"Fetch Secure Data"**.
*   **Result**: You should see an **Error** or **Access Denied** message (or the app might fail to validate the token).
*   **Script**: "Immediately, our security layer is gone. We lose multi-issuer validation, token inspection, and standardized security policies. The app is now vulnerable or non-functional."
*   **Restore**: Uncomment **Line 68** and Save. Show "Fetch Secure Data" working again.

---

### 3. Logging Demo

*   **Script**: "Now, let's look at observability. Primus Logging gives us structured logs, PII redaction, and correlation IDs out of the box."
*   **Action (VS Code)**:
    *   Go to **Line 46**.
    *   **Comment out**: `// builder.Logging.AddPrimus(...)`
    *   **Save** the file.
*   **Action (Browser)**:
    *   Go to the **Logs** tab.
    *   Click **"Refresh Logs"** or perform some actions.
*   **Result**: The logs will either stop appearing, lose their structured format (JSON), or lose the Correlation ID.
*   **Script**: "Without Primus Logging, we're back to plain text logs. We lose the ability to trace a request across services (Correlation ID) and automatically mask sensitive data."
*   **Restore**: Uncomment **Line 46** and Save.

---

### 4. Notifications Demo

*   **Script**: "Finally, the Notifications engine. This handles email, SMS, templates, and failover."
*   **Action (VS Code)**:
    *   Go to **Line 101**.
    *   **Comment out**: `// builder.Services.AddPrimusNotifications(...)`
    *   **Save** the file.
*   **Action (Browser)**:
    *   Go to the **Notifications** tab.
    *   Try to send a **"Welcome Email"**.
*   **Result**: The application will throw a **500 Internal Server Error** (check the Console or the red error box in the UI).
*   **Script**: "The entire notification capability is broken. We'd have to manually wire up SMTP clients, manage templates, and handle retries. Primus handles all of this with one line."
*   **Restore**: Uncomment **Line 101** and Save. Show the email sending successfully.

---

## Conclusion

*   **Script**: "By using these packages, we standardize our approach across all applications. We reduce code volume, improve security, and ensure consistent observability. This allows our developers to focus on business logic rather than infrastructure plumbing."
