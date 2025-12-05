# Primus.Documents Evaluation & Roadmap

## 1. Current Implementation Status

The `Primus.Documents` module is a functional **Text-to-PDF** rendering engine. It is built on top of `QuestPDF` (Community License).

### ✅ Implemented Features
- **Core Rendering**: Generates PDF files with headers, footers, and pagination.
- **Input Formats**: Accepts `PlainText`, `Markdown`, and `Html` inputs.
- **Architecture**: Clean, dependency-injected, multi-tenant aware.
- **Self-Testing**: Robust self-test subsystem (`IDocumentRendererSelfTest`) for verifying functionality in production.
- **Link Store**: Mechanism to generate temporary, tokenized download links.
- **Security**: Logging is sanitized (no content logging).

### ⚠️ Critical Limitations (The "Pending" List)
**The current implementation does NOT render Rich Text.**
- **Markdown**: It converts Markdown to HTML, and then **strips all HTML tags** to produce plain text.
- **HTML**: It **strips all HTML tags** and renders only the text content.
- **Formatting**: Bold, Italic, Lists, Tables, and Images are **lost** during rendering. The output is always a stream of plain text paragraphs.

**To be a competitive "Document Renderer", the following are pending:**
1.  **Rich Text Rendering**: Mapping Markdown/HTML tags to QuestPDF fluent API (e.g., `<b>` -> `.Bold()`).
2.  **Image Support**: Handling `<img>` tags or Markdown images.
3.  **Table Support**: Rendering HTML tables as PDF tables.
4.  **Font Management**: Support for custom fonts beyond the default.

---

## 2. Alternatives & Competitors (Context for NuGet Users)

When publishing to NuGet, users will compare `Primus.Documents` against:

### Commercial / Enterprise
- **IronPDF**: Uses a full Chrome engine to render HTML to PDF perfectly. Expensive license.
- **Aspose.PDF**: Heavyweight, enterprise-grade document manipulation.
- **iText 7**: Powerful, but complex licensing (AGPL/Commercial).

### Open Source / Free
- **PuppeteerSharp**: Controls a headless Chrome instance. Renders HTML perfectly but requires deploying a browser (heavy).
- **DinkToPdf / wkhtmltopdf**: Wrapper around older WebKit. Popular but unmaintained/deprecated.
- **QuestPDF**: The library we are using. Excellent for "Code-to-PDF" (building PDFs via C# fluent API), but requires manual work to support "HTML-to-PDF".

**Positioning `Primus.Documents`**: Currently, it is a lightweight **"Report Generator"** (Text/Logs to PDF) rather than a full "HTML-to-PDF" converter.

---

## 3. Communication Services Evaluation

### SMS: "Bypassing Twilio & Free SMS"
The user asked about implementing SMS without Twilio, specifically "full SMS to the world without any costs".

**Reality Check**:
- **"Free to the World" APIs do not exist** in the enterprise space. Telecom networks charge for termination.
- **Bypassing Twilio**: You can use AWS SNS or Azure Communication Services (already implemented), but they also cost money.

**The "Zero Cost" Workaround (Hacker/Indie Approach)**:
- **Android SMS Gateway**: You can install an app on an Android phone with an "Unlimited SMS" personal plan. The app acts as a server (Gateway).
    - **Pros**: Uses your existing flat-rate mobile plan (effectively "free" per message).
    - **Cons**: Low throughput, reliability depends on the phone being on/charged, violates carrier "fair use" policies if abused.
    - **Implementation**: We could implement an `ISmsSender` that posts to a local Android Gateway endpoint.

### Email: Alternatives
- **Current**: SMTP (Generic).
- **Alternatives (to implement later)**:
    - **SendGrid / Mailgun / Postmark**: Specialized APIs (better deliverability than raw SMTP).
    - **AWS SES**: Very low cost, high scale.
    - **Microsoft Graph API**: Sending via Office 365.

## 4. Recommendations
1.  **Clarify Scope**: Decide if `Primus.Documents` should support real Rich Text (requires significant dev effort to map HTML->QuestPDF) or remain a simple Text/Report renderer.
2.  **Update Docs**: Explicitly state that `Markdown` and `Html` inputs are currently converted to **Plain Text** to avoid misleading users.

---

## 5. Native / "No-Service" Implementation Guide (The "Bare Metal" Approach)

The user requested: *"using .net only without using twilio or other services"*.

### A. SMS: The GSM Modem Approach (Hardware Required)
It is **impossible** to send an SMS purely via software/internet without a gateway provider (like Twilio), because you must eventually interface with the cellular radio network.

**The Solution**: Use a **USB GSM Modem** (or a 4G Dongle) plugged into the server.
*   **How it works**: The modem acts as a mobile phone. You insert a standard SIM card.
*   **The .NET Implementation**:
    1.  Use `System.IO.Ports.SerialPort` to connect to the modem (e.g., `COM3`).
    2.  Send **AT Commands** (standard modem language) to the port.
    3.  **Command Sequence**:
        *   `AT` (Check connection) -> `OK`
        *   `AT+CMGF=1` (Set text mode) -> `OK`
        *   `AT+CMGS="+15550001234"` (Set recipient) -> `>`
        *   `[Message Body]` + `Ctrl+Z` (ASCII 26) to send.
*   **Cost**: Price of the hardware (~$30) + Standard SIM card plan (Unlimited SMS). **Zero API fees.**

### B. Email: Direct Delivery (The "Mail Server" Approach)
You can send emails using standard .NET (`MailKit` or `SmtpClient`), but "bypassing services" means you are not using a relay like SendGrid or Gmail.

**Option 1: Direct MX Delivery (Not Recommended)**
*   **Logic**: Your .NET app looks up the DNS `MX` record of the recipient (e.g., `gmail.com` -> `gmail-smtp-in.l.google.com`) and connects directly on Port 25.
*   **The Problem**: Residential and Cloud IPs are almost always blacklisted. Google/Outlook will reject your connection or mark it as spam immediately.

**Option 2: Local SMTP Relay (The Viable "No-Service" Way)**
*   **Logic**: Install a local SMTP server (like hMailServer or Postfix) on your machine.
*   **Configuration**: Point your .NET `SmtpClient` to `localhost`.
*   **Cost**: Free (Open Source software).
*   **Warning**: You still need a "Clean IP" and proper DNS records (SPF, DKIM, PTR) to ensure deliverability.

