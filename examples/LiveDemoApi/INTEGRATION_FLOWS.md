# Primus SaaS Integration Flows

> **Purpose**: Detailed sequence diagrams illustrating the integration and runtime flows for Primus SaaS modules. These diagrams cover the interactions between the Client, Frontend, Backend, Auth Provider, and Primus Portal.

---

## 🛡️ Module 1: Identity Validator

### 1. Runtime Integration Flow
This diagram shows the end-to-end authentication flow, from the user clicking "Login" to the backend validating the token and reporting to Primus Portal.

```mermaid
sequenceDiagram
    actor User as 👤 User
    participant FE as 💻 Frontend
    participant Auth as 🔐 Auth Provider<br/>(Auth0/Azure)
    participant BE as ⚙️ Backend API
    participant Primus as 🛡️ Primus Identity
    participant Portal as 🚀 Primus Portal

    User->>FE: Clicks Login
    FE->>Auth: Redirect for Authentication
    Auth-->>User: Login Page
    User->>Auth: Enter Credentials
    Auth-->>FE: Return Access Token (JWT)
    
    Note over FE, BE: Authenticated Request
    FE->>BE: API Request + Bearer Token
    
    BE->>Primus: Validate Token
    
    rect rgb(240, 248, 255)
        Note right of Primus: Validation Steps
        Primus->>Primus: Check Signature
        Primus->>Primus: Validate Issuer & Audience
        Primus->>Primus: Check Expiration
    end
    
    alt Token Valid
        Primus-->>BE: Token Valid + Claims
        
        par Telemetry (Async)
            Primus->>Portal: Report Active Session / Audit
        and Process Request
            BE->>BE: Process Business Logic
        end
        
        BE-->>FE: 200 OK + Data
        FE-->>User: Show Data
    else Token Invalid
        Primus-->>BE: Validation Failed
        BE-->>FE: 401 Unauthorized
        FE-->>User: Show Login Error
    end
```

### 2. Portal Configuration Flow
How a developer configures the Identity module in the Primus Portal.

```mermaid
sequenceDiagram
    actor Dev as 👨‍💻 Developer
    participant Portal as 🚀 Primus Portal
    participant Config as ⚙️ Configuration Service

    Dev->>Portal: Login to Dashboard
    Dev->>Portal: Create New Project
    Portal-->>Dev: Project Created (App ID)
    
    Dev->>Portal: Add Identity Provider
    Portal->>Dev: Select Provider (Auth0/Azure/OIDC)
    
    alt Auth0
        Dev->>Portal: Enter Domain & Audience
    else Azure AD
        Dev->>Portal: Enter Tenant ID & Client ID
    end
    
    Dev->>Portal: Save Configuration
    Portal->>Config: Validate & Store Settings
    Config-->>Portal: Config Saved
    
    Portal-->>Dev: Generate appsettings.json snippet
    Note right of Dev: Developer copies this<br/>to their project
```

### 3. Client Installation Flow
The steps a developer takes to install and integrate the package.

```mermaid
sequenceDiagram
    actor Dev as 👨‍💻 Developer
    participant IDE as 💻 VS Code / Visual Studio
    participant NuGet as 📦 NuGet Gallery
    participant App as 🚀 Application Code

    Note over Dev, App: Step 1: Install Package
    Dev->>IDE: dotnet add package PrimusSaaS.Identity.Validator
    IDE->>NuGet: Fetch Package
    NuGet-->>IDE: Package Installed

    Note over Dev, App: Step 2: Configure
    Dev->>IDE: Open appsettings.json
    IDE->>App: Add "PrimusIdentity" Section
    
    Note over Dev, App: Step 3: Integrate
    Dev->>IDE: Open Program.cs
    IDE->>App: Add builder.Services.AddPrimusIdentity(...)
    IDE->>App: Add app.UseAuthentication()
    IDE->>App: Add app.MapPrimusIdentityDiagnostics()
    
    Note over Dev, App: Step 4: Verify
    Dev->>IDE: Run Application
    IDE->>App: Start Web Host
    App-->>Dev: Application Running
```

---

## 📊 Module 2: Logging

### 1. Runtime Integration Flow
How logs flow from the application to the Primus Portal.

```mermaid
sequenceDiagram
    participant App as 🚀 Application
    participant Logger as 📊 Primus Logger
    participant Buffer as 📥 Async Buffer
    participant Portal as 🚀 Primus Portal
    participant Disk as 💾 Local Disk

    Note over App: Event Occurs
    App->>Logger: LogInformation("User {Email} logged in")
    
    rect rgb(255, 240, 245)
        Note right of Logger: Processing
        Logger->>Logger: Capture Context (RequestId, Scope)
        Logger->>Logger: Redact PII ({Email} -> [REDACTED])
        Logger->>Logger: Format as JSON
    end
    
    par Local Output
        Logger->>Disk: Write to rolling file
    and Remote Ingestion
        Logger->>Buffer: Enqueue Log Entry
    end
    
    loop Every 5 Seconds / Batch Size
        Buffer->>Portal: Bulk Ingest Logs (HTTPS)
        Portal-->>Buffer: 200 OK (Acknowledged)
    end
```

### 2. Portal View Flow
How a user interacts with logs in the Primus Portal.

```mermaid
sequenceDiagram
    actor User as 👤 Admin/Dev
    participant FE as 💻 Portal UI
    participant API as ⚙️ Portal API
    participant DB as 🗄️ Log Store

    User->>FE: Open Logs Dashboard
    FE->>API: Subscribe to Live Stream (WebSocket)
    API-->>FE: Connection Established
    
    loop Real-time Updates
        API-->>FE: Push New Log Entries
        FE-->>User: Update Grid
    end
    
    User->>FE: Search "Error" + Time Range
    FE->>API: Query Logs (Filter criteria)
    API->>DB: Execute Search
    DB-->>API: Return Matches
    API-->>FE: Return Results
    FE-->>User: Display Filtered Logs
```

### 3. Client Installation Flow

```mermaid
sequenceDiagram
    actor Dev as 👨‍💻 Developer
    participant IDE as 💻 IDE
    participant App as 🚀 Application

    Dev->>IDE: dotnet add package PrimusSaaS.Logging
    
    Dev->>IDE: Update appsettings.json
    Note right of Dev: Configure targets<br/>(Console, File, Portal)
    
    Dev->>IDE: Update Program.cs
    IDE->>App: builder.Logging.AddPrimus(...)
    
    Dev->>IDE: Run & Verify
    App->>Dev: Console shows JSON logs
```

---

## 🔔 Module 3: Notifications

### 1. Runtime Integration Flow
The flow of sending a notification through Primus.

```mermaid
sequenceDiagram
    participant App as 🚀 Application
    participant Service as 🔔 Notification Service
    participant Template as 🎨 Template Engine
    participant Dispatcher as 📤 Dispatcher
    participant Provider as ☁️ Provider (Twilio/SMTP)
    participant Portal as 🚀 Primus Portal

    App->>Service: SendAsync(NotificationRequest)
    
    Service->>Template: Load & Render Template
    Template-->>Service: Rendered Content (HTML/Text)
    
    Service->>Dispatcher: Dispatch(Content, Recipient)
    
    Dispatcher->>Portal: Check Quota / Track Request
    Portal-->>Dispatcher: Allowed
    
    Dispatcher->>Provider: Send API Call
    
    alt Success
        Provider-->>Dispatcher: 200 OK (MessageID)
        Dispatcher->>Portal: Update Status: Sent
        Dispatcher-->>Service: Success Result
        Service-->>App: Notification Sent
    else Failure
        Provider-->>Dispatcher: Error
        
        loop Retry Policy
            Dispatcher->>Dispatcher: Wait & Retry
            Dispatcher->>Provider: Retry Send
        end
        
        opt Fallback
            Dispatcher->>Dispatcher: Switch to Fallback Provider
            Dispatcher->>Provider: Send via Fallback
        end
        
        Dispatcher->>Portal: Update Status: Failed/Sent via Fallback
    end
```

### 2. Portal Template Flow
Managing templates in the portal.

```mermaid
sequenceDiagram
    actor User as 👤 Content Manager
    participant Portal as 🚀 Primus Portal
    participant Preview as 📱 Preview Engine
    participant App as 🚀 Client App

    User->>Portal: Create "Welcome Email" Template
    User->>Portal: Edit HTML/Text Content
    
    User->>Portal: Click Preview
    Portal->>Preview: Render with Sample Data
    Preview-->>User: Show Rendered Email
    
    User->>Portal: Publish Version 1.0
    
    Note over Portal, App: Sync Mechanism
    App->>Portal: Fetch Latest Templates (Startup/Periodic)
    Portal-->>App: Return Template Bundle
```

### 3. Client Installation Flow

```mermaid
sequenceDiagram
    actor Dev as 👨‍💻 Developer
    participant IDE as 💻 IDE
    participant App as 🚀 Application

    Dev->>IDE: dotnet add package PrimusSaaS.Notifications
    
    Dev->>IDE: Create Templates Folder
    IDE->>App: Add Welcome.html
    
    Dev->>IDE: Update appsettings.json
    Note right of Dev: Add SMTP/Twilio Creds
    
    Dev->>IDE: Update Program.cs
    IDE->>App: builder.Services.AddPrimusNotifications()
    
    Dev->>IDE: Inject INotificationService
    IDE->>App: Call SendAsync()
```
