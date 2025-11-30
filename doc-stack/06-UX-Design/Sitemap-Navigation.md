# 🗺️ Sitemap & Navigation Model

**App:** Live UI Demo / Portal

---

## 1. 🧭 Global Navigation (Sidebar)

| Icon | Label | Route | Description |
| :--- | :--- | :--- | :--- |
| 🏠 | **Dashboard** | `/` | System overview, health status. |
| 👤 | **Identity** | `/identity` | Login tests, token inspector. |
| 🔔 | **Notifications** | `/notifications` | Send test emails, view history. |
| 🪵 | **Logging** | `/logging` | Live log stream, PII demo. |
| ⚙️ | **Settings** | `/settings` | App configuration (Read-only). |

---

## 2. 👤 Identity Section

*   `/identity` (Landing)
    *   `/identity/login` (Login Page)
    *   `/identity/profile` (Protected User Profile)
    *   `/identity/debug` (JWT Debugger Tool)

---

## 3. 🔔 Notifications Section

*   `/notifications` (Landing)
    *   `/notifications/send` (Form to trigger email)
    *   `/notifications/templates` (Preview Liquid templates)
    *   `/notifications/history` (Log of sent messages)

---

## 4. 🪵 Logging Section

*   `/logging` (Landing)
    *   `/logging/stream` (WebSocket live tail)
    *   `/logging/search` (Filter logs by CorrelationId)
