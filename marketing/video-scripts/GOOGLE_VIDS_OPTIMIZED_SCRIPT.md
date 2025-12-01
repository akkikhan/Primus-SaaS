# Primus SaaS — Google Vids Scene-by-Scene Script

**Platform:** Google Vids (vids.google.com)  
**Avatar:** Samir (Clear, Low Pitch)  
**Target Duration:** 5-6 minutes  
**Format:** ~50-75 words per scene for optimal pacing

---

## HOW TO USE THIS SCRIPT

1. **Copy each scene's script** into the Google Vids scene panel
2. **Add visuals** — upload relevant images or use Google's stock media
3. **Preview each scene** before moving to the next
4. **Adjust pacing** — add pauses between scenes if needed
5. **Use transitions** — smooth fades work best for professional feel

---

## PART 1: THE HOOK (Scenes 1-3)

### SCENE 1: The Problem
**Words: 52 | Duration: ~20 sec**

```
Your development team just spent three months building authentication from scratch. 

Another two months on logging infrastructure. 

And now you're facing a deadline for email notifications that keeps slipping.

Sound familiar?
```

**Visual suggestion:** Developer looking frustrated at code, clock ticking

---

### SCENE 2: The Question
**Words: 35 | Duration: ~15 sec**

```
What if these common backend challenges could become one-line integrations instead of month-long projects?

What if infrastructure code just... worked?
```

**Visual suggestion:** Lightbulb moment, transformation graphic

---

### SCENE 3: The Answer
**Words: 48 | Duration: ~18 sec**

```
Welcome to Primus SaaS — the developer SDK platform that's changing how modern applications are built.

Production-ready backend modules. Zero vendor lock-in. Minutes to integrate.
```

**Visual suggestion:** Primus logo, modern tech aesthetic

---

## PART 2: WHAT IS PRIMUS? (Scenes 4-7)

### SCENE 4: Platform Overview
**Words: 55 | Duration: ~22 sec**

```
Primus SaaS provides battle-tested backend modules as NuGet and npm packages.

Think of it as your backend infrastructure library — authentication, logging, notifications, and more — all ready to drop into your application.
```

**Visual suggestion:** Package boxes/modules graphic, NuGet + npm logos

---

### SCENE 5: Zero Runtime Dependency
**Words: 52 | Duration: ~20 sec**

```
Here's what makes Primus different.

First, zero runtime dependency. Our modules run entirely inside YOUR application. No external API calls. No cloud services processing your data. Your data never leaves your infrastructure.
```

**Visual suggestion:** Data staying inside a secure boundary, no external arrows

---

### SCENE 6: Privacy by Design
**Words: 45 | Duration: ~18 sec**

```
Second, privacy by design. We never store or process your end-user data, tokens, or personal information. Ever.

This makes compliance with GDPR, HIPAA, and other regulations dramatically simpler.
```

**Visual suggestion:** Shield icon, compliance badges (GDPR, HIPAA)

---

### SCENE 7: Multi-Stack Support
**Words: 42 | Duration: ~16 sec**

```
Third, multi-stack support. Whether your team builds with .NET or Node.js, we've got you covered with native packages optimized for each ecosystem.

Let me show you what's available.
```

**Visual suggestion:** .NET and Node.js logos, code editor

---

## PART 3: IDENTITY VALIDATOR (Scenes 8-12)

### SCENE 8: Identity — The Problem
**Words: 58 | Duration: ~22 sec**

```
Let's start with authentication — the Identity Validator module.

Building authentication traditionally means writing hundreds of lines of JWT validation code, implementing key caching, handling security vulnerabilities, and supporting multiple identity providers.

This typically takes two to three months.
```

**Visual suggestion:** Complex authentication flowchart, lots of code

---

### SCENE 9: Identity — The Solution
**Words: 38 | Duration: ~15 sec**

```
With Primus Identity Validator, here's your entire authentication setup:

builder.Services.AddPrimusIdentity with your configuration options.

That's it. One line of code. Authentication done.
```

**Visual suggestion:** Single line of code highlighted, checkmark

---

### SCENE 10: Identity — Features Part 1
**Words: 52 | Duration: ~20 sec**

```
What do you get?

Multi-issuer JWT validation — support Azure AD, Auth0, Okta, or your own local tokens. All simultaneously in the same application.

Automatic public key caching for optimal performance.
```

**Visual suggestion:** Multiple identity provider logos connecting to one app

---

### SCENE 11: Identity — Features Part 2
**Words: 48 | Duration: ~18 sec**

```
Comprehensive security validation — signature verification, issuer checks, audience validation, expiry handling, and algorithm protection. All built-in.

Hybrid authentication — mix cloud providers with local authentication seamlessly.
```

**Visual suggestion:** Security checklist with green checkmarks

---

### SCENE 12: Identity — Performance
**Words: 40 | Duration: ~15 sec**

```
The performance? Under one millisecond for cached tokens. Under fifty milliseconds on cache miss.

This is authentication that just works — secure by default, configurable when you need it.
```

**Visual suggestion:** Speedometer showing fast performance, milliseconds counter

---

## PART 4: LOGGING MODULE (Scenes 13-17)

### SCENE 13: Logging — The Problem
**Words: 50 | Duration: ~20 sec**

```
Next up — the Logging module.

Setting up proper logging infrastructure means configuring multiple providers, building JSON formatters, implementing PII redaction, and setting up file rotation.

That's easily a month of dedicated infrastructure work.
```

**Visual suggestion:** Complex logging setup diagram, multiple config files

---

### SCENE 14: Logging — The Solution
**Words: 35 | Duration: ~14 sec**

```
With Primus Logging:

builder.Logging.AddPrimus with your configuration.

One line. Enterprise-grade logging. Done.
```

**Visual suggestion:** Single line of code, logging dashboard preview

---

### SCENE 15: Logging — Structured Output
**Words: 55 | Duration: ~22 sec**

```
You get structured JSON logging with every entry properly formatted.

Timestamp, log level, application ID, message, and rich context — all automatically structured and ready for analysis in any log aggregation tool.
```

**Visual suggestion:** Beautiful JSON log entry displayed

---

### SCENE 16: Logging — PII Protection
**Words: 52 | Duration: ~20 sec**

```
Here's the game-changer — automatic PII masking.

Email addresses, credit card numbers, social security numbers — all automatically redacted before they hit your logs.

No manual configuration. No compliance headaches. Just protection built-in.
```

**Visual suggestion:** Email being masked: "john@email.com" → "j***@***.com"

---

### SCENE 17: Logging — Multiple Targets
**Words: 45 | Duration: ~18 sec**

```
Output to console, file, and Azure Application Insights — all simultaneously.

File rotation with compression, async buffering for performance, and HTTP context enrichment including request IDs and correlation tracking.
```

**Visual suggestion:** Arrows pointing to Console, File, and Application Insights icons

---

## PART 5: NOTIFICATIONS MODULE (Scenes 18-22)

### SCENE 18: Notifications — The Problem
**Words: 48 | Duration: ~18 sec**

```
The Notifications module transforms how you handle user communications.

Traditional notification systems require SMTP setup, SMS provider integration, HTML templates in code, and complex retry logic.

Weeks of work. Endless maintenance.
```

**Visual suggestion:** Tangled mess of email/SMS code, frustrated developer

---

### SCENE 19: Notifications — The Solution
**Words: 42 | Duration: ~16 sec**

```
With Primus Notifications:

AddPrimusNotifications, configure your SMTP, point to your templates folder, and you're done.

Multi-channel notifications in minutes.
```

**Visual suggestion:** Clean code snippet, email and SMS icons

---

### SCENE 20: Notifications — Liquid Templates
**Words: 55 | Duration: ~22 sec**

```
The magic is in Liquid templating.

Store your email and SMS templates as simple files. Edit them without recompiling your application.

Your marketing team can update email copy without involving developers. Just save the file and the changes are live.
```

**Visual suggestion:** Template file structure, Liquid syntax example

---

### SCENE 21: Notifications — Multi-Channel
**Words: 50 | Duration: ~20 sec**

```
Send email via SMTP, SMS via Twilio, AWS SNS, or Azure Communication Services.

Need to switch providers? Change the configuration. Your application code stays exactly the same.

True provider flexibility without vendor lock-in.
```

**Visual suggestion:** Provider logos (Twilio, AWS, Azure) with swap arrows

---

### SCENE 22: Notifications — Simple API
**Words: 38 | Duration: ~15 sec**

```
Sending a notification becomes one line:

await notifier.SendAsync with your notification object.

Password resets, welcome emails, order confirmations — all the same simple pattern.
```

**Visual suggestion:** Single SendAsync line, multiple notification types flowing out

---

## PART 6: COMING SOON (Scenes 23-25)

### SCENE 23: Feature Flags Preview
**Words: 58 | Duration: ~22 sec**

```
We're not stopping here. Coming soon — the Feature Flags module, already in preview.

Control feature rollouts with precision. Release to five percent of users, then twenty-five, then everyone. Target specific users or groups. Schedule activation windows.

Controlled releases made simple.
```

**Visual suggestion:** Percentage slider, user targeting diagram

---

### SCENE 24: Document Renderer
**Words: 45 | Duration: ~18 sec**

```
Also in preview — the Document Renderer module.

Generate professional PDF documents from Markdown, HTML, or plain text.

Perfect for invoices, reports, and dynamic documentation. All running inside your application with no external services.
```

**Visual suggestion:** Markdown transforming into beautiful PDF

---

### SCENE 25: On the Roadmap
**Words: 42 | Duration: ~16 sec**

```
On our roadmap: Role-Based Access Control for fine-grained permissions, Audit Logging for compliance tracking, Rate Limiting to protect your APIs, and standardized Health Check endpoints.

Each module follows the same Primus philosophy.
```

**Visual suggestion:** Roadmap timeline with module icons

---

## PART 7: THE BUSINESS CASE (Scenes 26-28)

### SCENE 26: Time Savings
**Words: 52 | Duration: ~20 sec**

```
Let's talk business impact.

Authentication that takes three months? Done in a day with Primus.
Logging infrastructure that takes a month? An afternoon.
Notification systems that take weeks? Hours.

Teams reduce infrastructure development time by seventy to eighty percent.
```

**Visual suggestion:** Before/after timeline comparison, dramatic time reduction

---

### SCENE 27: Risk & Compliance
**Words: 55 | Duration: ~22 sec**

```
Security vulnerabilities in custom authentication code are common and costly. Our modules are battle-tested with comprehensive test suites.

With automatic PII masking and zero external data processing, compliance reviews become dramatically simpler. GDPR, HIPAA, SOC 2 — all easier conversations.
```

**Visual suggestion:** Security shield, compliance checkmarks

---

### SCENE 28: Developer Happiness
**Words: 45 | Duration: ~18 sec**

```
Your developers want to build features that matter, not reinvent authentication for the hundredth time.

Primus lets them focus on what makes your application unique.

Faster time to market. Reduced risk. Lower maintenance. Happier teams.
```

**Visual suggestion:** Happy developer, feature launches, rocket ship

---

## PART 8: GETTING STARTED & CLOSE (Scenes 29-31)

### SCENE 29: Getting Started
**Words: 50 | Duration: ~20 sec**

```
Ready to get started?

Install the packages you need with dotnet add package. Add your configuration to appsettings.json. Wire up the services in Program.cs.

That's it. You're ready to build features instead of infrastructure.
```

**Visual suggestion:** Terminal showing dotnet add package command, config file

---

### SCENE 30: Resources
**Words: 45 | Duration: ~18 sec**

```
Our documentation site provides step-by-step guides, live code examples, and a fully working demo application.

Comprehensive API references, troubleshooting guides, and golden path examples show you the fastest way to integrate each module.
```

**Visual suggestion:** Documentation site preview, code examples

---

### SCENE 31: Closing
**Words: 48 | Duration: ~18 sec**

```
Primus SaaS — production-ready modules, zero vendor lock-in, minutes to integrate.

Stop building infrastructure. Start building your product.

Visit our documentation to explore the platform and try our live demo today.

Thank you for watching.
```

**Visual suggestion:** Primus logo, call-to-action button, professional outro

---

## QUICK REFERENCE CARD

| Part | Scenes | Duration | Topic |
|------|--------|----------|-------|
| Hook | 1-3 | ~53 sec | Problem & Solution intro |
| What is Primus | 4-7 | ~76 sec | Platform overview |
| Identity | 8-12 | ~90 sec | Authentication module |
| Logging | 13-17 | ~94 sec | Logging module |
| Notifications | 18-22 | ~91 sec | Notifications module |
| Coming Soon | 23-25 | ~56 sec | Future modules |
| Business Case | 26-28 | ~60 sec | ROI & benefits |
| Close | 29-31 | ~56 sec | Getting started & CTA |

**Total: 31 scenes | ~9-10 minutes**

---

## PRO TIPS FOR GOOGLE VIDS

### 1. **Scene Flow**
- Copy ONE scene at a time into the script panel
- Preview the AI avatar reading it
- Adjust if pacing feels off

### 2. **Visuals**
- Use Google Vids' stock images or upload your own
- Keep visuals simple — don't compete with the narration
- Use consistent color scheme (match Primus branding)

### 3. **Transitions**
- Use "Fade" transitions for professional feel
- Avoid flashy transitions — they distract
- Keep 0.5-1 second between scenes

### 4. **Shorter Version**
To create a 5-minute version, use only these scenes:
- Scenes 1-3 (Hook)
- Scenes 4, 5 (What is Primus)
- Scenes 8, 9, 10 (Identity highlights)
- Scenes 13, 14, 16 (Logging highlights)
- Scenes 18, 19, 20 (Notifications highlights)
- Scenes 23, 24 (Coming soon)
- Scenes 26, 28 (Business case)
- Scenes 29, 31 (Close)

### 5. **Music**
- Add subtle background music (Google Vids has options)
- Keep volume at 10-15% — don't overpower the voice
- Use upbeat but professional tracks

---

*Optimized for Google Vids AI Avatar Narration*  
*Version 1.0 | November 30, 2025*
