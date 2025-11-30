# Primus SaaS Portal - Backend API

ASP.NET Core 8.0 Web API for the Primus SaaS Platform portal.

## Features

- **Authentication**: JWT-based authentication for admin users
- **Module Management**: CRUD operations for modules and versions
- **Application Registry**: Manage client applications and their integrated modules
- **Documentation Generation**: Auto-generate integration docs with code snippets
- **Database**: Entity Framework Core with SQL Server
- **Notifications**: Email on app creation, module assignment, and version updates (major-only)

## Dependencies snapshot

### Backend (.NET 8)

- `AspNetCoreRateLimit` 5.0.0
- `BCrypt.Net-Next` 4.0.3
- `Microsoft.ApplicationInsights.AspNetCore` 2.23.0
- `Microsoft.AspNetCore.Authentication.JwtBearer` 8.0.0
- `Microsoft.AspNetCore.OpenApi` 8.0.0
- `Microsoft.EntityFrameworkCore.Design` 8.0.0
- `Microsoft.EntityFrameworkCore.Sqlite` 8.0.0
- `Microsoft.EntityFrameworkCore.SqlServer` 8.0.0
- `Microsoft.EntityFrameworkCore.Tools` 8.0.0
- `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` 8.0.0
- `Microsoft.Identity.Web` 4.1.0
- `Microsoft.IdentityModel.Protocols.OpenIdConnect` 8.15.0
- `Npgsql.EntityFrameworkCore.PostgreSQL` 8.0.0
- `QuestPDF` 2024.7.0
- `Serilog.AspNetCore` 8.0.2
- `Serilog.Enrichers.Environment` 2.2.0
- `Serilog.Sinks.ApplicationInsights` 4.1.0
- `Serilog.Sinks.Console` 5.0.1
- `Serilog.Sinks.File` 5.0.0
- `Swashbuckle.AspNetCore` 6.5.0

### Frontend (portal/frontend)

- `react` 18.3.1
- `react-dom` 18.3.1
- `react-router-dom` 6.28.0
- `@azure/msal-browser` 4.26.2
- `@azure/msal-react` 3.0.22
- `@emotion/react` 11.13.5
- `@emotion/styled` 11.13.5
- `@mui/icons-material` 7.3.5
- `@mui/material` 7.3.5
- `zustand` 4.5.6
- `axios` 1.7.7
- `file-saver` 2.0.5
- `jspdf` 3.0.4

#### Frontend dev tools

- `vite` 5.4.8
- `typescript` 5.6.3
- `vitest` 3.2.4
- `@vitejs/plugin-react` 4.2.1
- `eslint` 9.13.0 with `@typescript-eslint` 8.10.0 / 8.47.0 plugins
- `@testing-library/react` 16.3.0 / `@testing-library/jest-dom` 6.9.1

## Prerequisites

- .NET 8.0 SDK or higher
- Node.js 20+ (for building the portal frontend)
- SQL Server (LocalDB or full instance)

## Getting Started

### 1. Configure Database Provider and Connection

The API now reads `ConnectionStrings__DefaultConnection` and `DatabaseProvider` (`SqlServer` by default, `Postgres` or `Sqlite` for local dev). Update `appsettings*.json` or environment variables accordingly:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=PrimusSaasPortal;User Id=sa;Password=ChangeMe123!;TrustServerCertificate=True;"
},
"DatabaseProvider": "SqlServer"
```

### 2. Seed an Admin User

Provide credentials via configuration (recommended: environment variables) before first run:

```
SeedAdmin__Email=admin@example.com
SeedAdmin__Password=StrongPasswordHere
```

### 3. Create Database

Run EF Core migrations to create the database:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 4. Run the API

```bash
dotnet run
```

The API will start at:

- HTTPS: `https://localhost:7001`
- HTTP: `http://localhost:5001`

Swagger UI available at: `https://localhost:7001/swagger`

### 5. Email/Docs configuration

- Set SMTP and docs host in `appsettings.json`:
  ```json
  "EmailSettings": {
    "SmtpHost": "...",
    "SmtpPort": 587,
    "SmtpUser": "...",
    "SmtpPass": "...",
    "EnableSsl": true,
    "FromAddress": "...",
    "DocsBaseUrl": "http://localhost:3001" // update when docs are hosted
  }
  ```

## API Endpoints

### Authentication

- `POST /api/auth/login` - Login and receive JWT token

### Modules

- `GET /api/modules` - List all modules
- `GET /api/modules/{id}` - Get module details with versions
- `POST /api/modules` - Create new module
- `POST /api/modules/{id}/versions` - Add new version to module
- `PUT /api/modules/{id}` - Update module
- `DELETE /api/modules/{id}` - Delete module

### Applications

- `GET /api/applications` - List all applications
- `GET /api/applications/{id}` - Get application details
- `POST /api/applications` - Register new application
- `POST /api/applications/{id}/modules` - Integrate module into application
- `POST /api/applications/{id}/modules/{moduleId}/version` - Change module version
- `DELETE /api/applications/{id}` - Delete application

### Documentation

- `GET /api/documentation/{applicationId}` - Generate integration documentation with code snippets

## Database Schema

### Users Table

- Id, Email, PasswordHash, Role, CreatedAt, UpdatedAt

### Modules Table

- Id, Name, Description

### ModuleVersions Table

- Id, ModuleId, Version, IsBreakingChange, ReleaseNotes, SupportedStacksJson, ReleasedAt

### Applications Table

- Id, OwnerUserId, Name, Stack, PrimusClientId, CreatedAt, UpdatedAt

### ApplicationModules Table

- Id, ApplicationId, ModuleId, ModuleVersionId, ConfigJson, IntegratedAt

## Project Structure

```text
portal/backend/
├── Controllers/
│   ├── AuthController.cs         # JWT authentication
│   ├── ModulesController.cs      # Module CRUD
│   ├── ApplicationsController.cs # Application registry
│   └── DocumentationController.cs # Documentation generation
├── Data/
│   └── PortalDbContext.cs        # EF Core DbContext
├── Models/
│   ├── User.cs
│   ├── Module.cs
│   ├── ModuleVersion.cs
│   ├── Application.cs
│   └── ApplicationModule.cs
├── Program.cs                     # App configuration
└── appsettings.json               # Configuration
```

## Configuration

### JWT Settings

Edit in `appsettings.json`:

```json
"Jwt": {
  "Key": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
  "Issuer": "PrimusSaasPortal",
  "Audience": "PrimusSaasPortalUsers",
  "ExpiryInMinutes": 60
}
```

### CORS

Currently configured to allow all origins for development. Update in `Program.cs` for production:

```csharp
options.AddPolicy("AllowAll", policy =>
{
    policy.WithOrigins("https://your-frontend-domain.com")
          .AllowAnyMethod()
          .AllowAnyHeader();
});
```

## Next Steps

1. **Implement BCrypt password hashing** in `AuthController`
2. **Create EF migrations** and update database
3. **Build React frontend** to consume this API
4. **Add email notification service** for update alerts
5. **Implement rate limiting** for API endpoints
6. **Add logging and monitoring** (Application Insights, Serilog) — see `OBSERVABILITY.md` for setup and alert templates.

## Development

### Add New Migration

```bash
dotnet ef migrations add MigrationName
dotnet ef database update
```

### Remove Last Migration

```bash
dotnet ef migrations remove
```

### Reset Database

```bash
dotnet ef database drop
dotnet ef database update
```

## Technologies

- **Framework**: ASP.NET Core 7.0
- **ORM**: Entity Framework Core 7.0
- **Database**: SQL Server
- **Authentication**: JWT Bearer tokens
- **API Documentation**: Swagger/OpenAPI
