# 📡 Primus SaaS — API Reference

**Version:** 1.0  
**Format:** REST / JSON  
**Auth:** Bearer JWT

---

## 1. 🌐 Common Contracts

### Standard Response Envelope
All API responses follow this structure for consistency:

```json
{
  "data": { ... },       // The actual payload
  "meta": {
    "correlationId": "12345-abcde",
    "timestamp": "2023-10-27T10:00:00Z"
  },
  "error": null          // Null on success, object on failure
}
```

### Standard Error Object
```json
{
  "code": "RESOURCE_NOT_FOUND",
  "message": "The requested user ID was not found.",
  "details": [ "User ID 555 does not exist in tenant A" ]
}
```

---

## 2. 👤 Identity Endpoints

### `GET /api/identity/whoami`
Returns the claims of the currently authenticated user. Used to validate token processing.

**Request:**
*   Headers: `Authorization: Bearer <token>`

**Response (200 OK):**
```json
{
  "sub": "auth0|123456",
  "name": "Alice Developer",
  "email": "alice@example.com",
  "iss": "https://dev-tenant.auth0.com/",
  "aud": "primus-api"
}
```

---

## 3. 🔔 Notification Endpoints

### `POST /api/notifications/send`
Triggers a transactional notification.

**Request:**
```json
{
  "templateName": "PasswordReset",
  "recipient": {
    "email": "bob@example.com",
    "name": "Bob Smith"
  },
  "data": {
    "code": "998877",
    "link": "https://app.primus.com/reset?token=xyz"
  }
}
```

**Response (202 Accepted):**
```json
{
  "status": "Queued",
  "provider": "SMTP",
  "id": "msg_987654321"
}
```

---

## 4. 🪵 Logging (Internal)

The logging module does not expose HTTP endpoints but produces **Log Entries** in this format:

```json
{
  "Timestamp": "2023-10-27T10:05:00Z",
  "Level": "Information",
  "MessageTemplate": "User {UserId} requested {Action}",
  "Properties": {
    "UserId": "user_123",
    "Action": "ExportReport",
    "CorrelationId": "corr_abc123",
    "SourceContext": "Primus.Reporting.Service"
  }
}
```
