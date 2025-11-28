# Primus SaaS Real-World Demo API

A complete e-commerce API demonstrating real-world usage of:
- `@primus-saas/identity-validator` for authentication
- `@primus-saas/logging` for structured logging with PII masking

## Features

### Authentication & Authorization
- ✅ User registration
- ✅ JWT-based login
- ✅ Protected routes with middleware
- ✅ Role-based access control (admin vs user)

### Business Logic
- ✅ Product catalog (public)
- ✅ Order management (authenticated users)
- ✅ Admin dashboard (admin role only)
- ✅ Stock management

### Logging & Security
- ✅ Request/response logging
- ✅ PII masking for sensitive data
- ✅ File and console logging
- ✅ Structured log entries

## Quick Start

### 1. Install Dependencies
```bash
npm install
```

### 2. Configure Environment
Copy `.env.example` to `.env` (or use the existing one)

### 3. Start the Server
```bash
npm run dev
```

Server will start on http://localhost:4000

## API Endpoints

### Public Endpoints

#### GET /
Get API information
```bash
curl http://localhost:4000/
```

#### GET /products
List all products
```bash
curl http://localhost:4000/products
```

#### GET /products/:id
Get product details
```bash
curl http://localhost:4000/products/1
```

#### POST /auth/register
Register a new user
```bash
curl -X POST http://localhost:4000/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "newuser@demo.com",
    "name": "New User",
    "password": "password123"
  }'
```

#### POST /auth/login
Login and get JWT token
```bash
curl -X POST http://localhost:4000/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@demo.com",
    "password": "user123"
  }'
```

**Response:**
```json
{
  "message": "Login successful",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": "2",
    "email": "user@demo.com",
    "name": "Regular User",
    "role": "user"
  }
}
```

### Protected Endpoints (Require Authentication)

**Note:** Add the token from login to the Authorization header:
```
Authorization: Bearer <your-token-here>
```

#### POST /orders
Create a new order
```bash
curl -X POST http://localhost:4000/orders \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -d '{
    "items": [
      { "productId": "1", "quantity": 1 },
      { "productId": "2", "quantity": 2 }
    ]
  }'
```

#### GET /orders
Get user's orders
```bash
curl http://localhost:4000/orders \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

#### GET /orders/:id
Get specific order details
```bash
curl http://localhost:4000/orders/1 \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

### Admin Endpoints (Require Admin Role)

**Login as admin first:**
```bash
curl -X POST http://localhost:4000/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@demo.com",
    "password": "admin123"
  }'
```

#### GET /admin/users
List all users
```bash
curl http://localhost:4000/admin/users \
  -H "Authorization: Bearer ADMIN_TOKEN_HERE"
```

#### GET /admin/stats
Get system statistics
```bash
curl http://localhost:4000/admin/stats \
  -H "Authorization: Bearer ADMIN_TOKEN_HERE"
```

## Test Users

| Email | Password | Role |
|-------|----------|------|
| admin@demo.com | admin123 | admin |
| user@demo.com | user123 | user |

## Complete Test Flow

### 1. Login as Regular User
```bash
# Login
curl -X POST http://localhost:4000/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "user@demo.com", "password": "user123"}'

# Save the token from response
export TOKEN="<token-from-response>"
```

### 2. Browse Products
```bash
curl http://localhost:4000/products
```

### 3. Create an Order
```bash
curl -X POST http://localhost:4000/orders \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "items": [
      { "productId": "1", "quantity": 1 },
      { "productId": "2", "quantity": 2 }
    ]
  }'
```

### 4. View Your Orders
```bash
curl http://localhost:4000/orders \
  -H "Authorization: Bearer $TOKEN"
```

### 5. Try Admin Access (Should Fail)
```bash
curl http://localhost:4000/admin/users \
  -H "Authorization: Bearer $TOKEN"
# Expected: 403 Forbidden
```

### 6. Login as Admin
```bash
curl -X POST http://localhost:4000/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "admin@demo.com", "password": "admin123"}'

export ADMIN_TOKEN="<admin-token-from-response>"
```

### 7. Access Admin Endpoints
```bash
# View all users
curl http://localhost:4000/admin/users \
  -H "Authorization: Bearer $ADMIN_TOKEN"

# View statistics
curl http://localhost:4000/admin/stats \
  -H "Authorization: Bearer $ADMIN_TOKEN"
```

## What This Demonstrates

### Identity Validator Features
✅ **Multi-issuer JWT validation** - Configured with custom issuer  
✅ **Express middleware integration** - `primusIdentityMiddleware()`  
✅ **Token validation** - Automatic validation on protected routes  
✅ **Claims extraction** - User info extracted from JWT  
✅ **Error handling** - Invalid/expired tokens rejected  

### Logging Features
✅ **Structured logging** - All logs include context  
✅ **Multiple targets** - Console (pretty) + File (JSON)  
✅ **PII masking** - Email addresses masked in logs  
✅ **Request logging** - Automatic HTTP request/response logging  
✅ **Log levels** - DEBUG, INFO, WARNING, ERROR used appropriately  
✅ **Context enrichment** - User IDs, request IDs, etc.  

### Real-World Patterns
✅ **Authentication flow** - Register → Login → Get Token → Use Token  
✅ **Authorization** - Role-based access control  
✅ **Business logic** - Order processing with stock management  
✅ **Error handling** - Proper HTTP status codes and error messages  
✅ **Security** - Protected routes, admin-only endpoints  

## Logs

Check the `./logs` directory for structured log files. Example log entry:

```json
{
  "timestamp": "2025-11-26T08:33:00.000Z",
  "level": "INFO",
  "message": "Order created successfully",
  "context": {
    "applicationId": "primus-demo",
    "environment": "development",
    "orderId": "1",
    "userId": "2",
    "total": 1059.97,
    "itemCount": 2
  }
}
```

## Production Considerations

This is a demo application. For production:

1. **Security:**
   - Hash passwords (use bcrypt)
   - Use strong JWT secrets
   - Implement rate limiting
   - Add CORS configuration
   - Use HTTPS

2. **Database:**
   - Replace in-memory storage with real database
   - Add data validation
   - Implement transactions

3. **Logging:**
   - Configure log retention
   - Set up log aggregation (e.g., ELK stack)
   - Add performance monitoring

4. **Error Handling:**
   - Add global error handler
   - Implement retry logic
   - Add circuit breakers

## License

MIT
