# 🖥️ Figma-style Screens (Wireframes)

**Scope:** Portal & Live Demo

---

## 1. 🏠 Dashboard Screen

```text
+-------------------------------------------------------+
|  PRIMUS SAAS  [Dashboard] [Identity] [Logs]    (User) |
+-------------------------------------------------------+
|                                                       |
|  👋 Welcome back, Akki!                               |
|                                                       |
|  +-------------------+   +-----------------------+    |
|  | System Status     |   | Recent Activity       |    |
|  | [🟢 Healthy]      |   | • User Login (1m ago) |    |
|  | Uptime: 99.9%     |   | • Email Sent (5m ago) |    |
|  +-------------------+   +-----------------------+    |
|                                                       |
|  +--------------------------------------------------+ |
|  |  Module Health                                   | |
|  |  [Identity]  🟢 Active  (Azure AD, Auth0)        | |
|  |  [Logging]   🟢 Active  (Console, File)          | |
|  |  [Notify]    🟡 Warning (SMTP Slow)              | |
|  +--------------------------------------------------+ |
|                                                       |
+-------------------------------------------------------+
```

---

## 2. 🔔 Send Notification Screen

```text
+-------------------------------------------------------+
|  < Back to Notifications                              |
+-------------------------------------------------------+
|  📨 Send Test Email                                   |
|                                                       |
|  Template: [ Password Reset (v1) ▼ ]                  |
|                                                       |
|  Recipient: [ test@example.com      ]                 |
|                                                       |
|  Payload Data (JSON):                                 |
|  +-------------------------------------------------+  |
|  | {                                               |  |
|  |   "code": "123456",                             |  |
|  |   "link": "https://..."                         |  |
|  | }                                               |  |
|  +-------------------------------------------------+  |
|                                                       |
|  [ Send Email ]  [ Preview Template ]                 |
|                                                       |
+-------------------------------------------------------+
```
