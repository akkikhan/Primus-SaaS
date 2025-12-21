# Testing the Primus Notification Module

## Prerequisites
*   **Frontend**: Running on `http://localhost:5173` (You already have this running).
*   **Backend**: Must be running on `http://localhost:5000`.

## Step 1: Start the Backend
Since the .NET SDK is not available in the current agent shell, you must start the backend manually in your terminal:

1.  Open a new terminal window.
2.  Navigate to the backend directory:
    ```bash
    cd "c:\Users\Akki\Primus SaaS\portal\backend"
    ```
3.  Run the application:
    ```bash
    dotnet run
    ```
    *Wait for it to say "Now listening on: http://localhost:5000"*

## Step 2: Verify Frontend Integration
1.  Open your browser to [http://localhost:5173](http://localhost:5173).
2.  You should see a new **"Notifications"** link in the sidebar.
3.  Click it to open the **Notification Center**.

## Step 3: Run the Test
1.  On the Notification Center page, look for the **"Test Dispatcher"** card.
2.  Click the blue **"Send Test Notification"** button.
3.  **Observe**:
    *   **Frontend**: A green success toast appears. The "Live Activity" log updates.
    *   **Backend Terminal**: You will see a log entry from the `LoggerChannel`:
        ```
        info: PrimusSaaS.Notifications.Channels.LoggerChannel[0]
              📢 [NOTIFICATION] Type: ApplicationCreated | Recipient: test@example.com ...
        ```

## Troubleshooting
*   **Button spins forever?** Check if the backend is running on port 5000.
*   **Red Error Toast?** Check the browser console (F12) for network errors. Ensure CORS is enabled (it is configured in `Program.cs`).
