# Local Development Guide - Primus SaaS

This guide helps you run Primus SaaS locally without Azure infrastructure.

## Prerequisites

- [.NET 7.0 SDK](https://dotnet.microsoft.com/download/dotnet/7.0)
- [Node.js 18+](https://nodejs.org/)
- [Git](https://git-scm.com/)
- Code editor (VS Code recommended)

## Architecture (Local)

```
┌─────────────────────────────────────────────────────┐
│                  Local Development                  │
├─────────────────────────────────────────────────────┤
│                                                     │
│  Frontend (React + Vite)                            │
│  http://localhost:5173                              │
│         │                                           │
│         │ HTTP Requests                             │
│         ▼                                           │
│  Backend API (.NET 7.0)                             │
│  http://localhost:5241                              │
│         │                                           │
│         │ Entity Framework Core                     │
│         ▼                                           │
│  SQLite Database                                    │
│  portal/backend/primus-portal.db                    │
│                                                     │
└─────────────────────────────────────────────────────┘
```

## Quick Start

### 1. Clone and Setup

```bash
git clone https://github.com/akkikhan/Primus-SaaS.git
cd Primus-SaaS
```

### 2. Backend Setup

```bash
cd portal/backend

# Restore dependencies
dotnet restore

# Run database migrations
dotnet ef database update

# Start the API
dotnet run
```

The backend API will start at: **http://localhost:5241**

### 3. Frontend Setup (New Terminal)

```bash
cd portal/frontend

# Install dependencies
npm install

# Start development server
npm run dev
```

The frontend will start at: **http://localhost:5173**

### 4. Access the Portal

Open your browser and navigate to:
- **Frontend**: http://localhost:5173
- **API**: http://localhost:5241/swagger (API documentation)

**Default Admin Credentials:**
- Email: `admin@primus.com`
- Password: `Admin123!`

## Development Workflow

### Backend Development

```bash
cd portal/backend

# Watch mode (auto-reload on changes)
dotnet watch run

# Run tests
dotnet test

# Create new migration
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update

# Remove last migration
dotnet ef migrations remove
```

### Frontend Development

```bash
cd portal/frontend

# Development server with hot reload
npm run dev

# Run linter
npm run lint

# Run tests
npm test

# Build for production
npm run build

# Preview production build
npm run preview
```

### Database Management

**View/Edit Database:**
- Install [DB Browser for SQLite](https://sqlitebrowser.org/)
- Open `portal/backend/primus-portal.db`

**Reset Database:**
```bash
cd portal/backend

# Delete database
rm primus-portal.db

# Recreate from migrations
dotnet ef database update
```

**Backup Database:**
```bash
cd portal/backend
cp primus-portal.db primus-portal-backup-$(Get-Date -Format "yyyy-MM-dd").db
```

## Project Structure

```
Primus SaaS/
├── portal/
│   ├── backend/                    # ASP.NET Core API
│   │   ├── Controllers/            # API endpoints
│   │   ├── Models/                 # Data models
│   │   ├── Data/                   # DbContext
│   │   ├── Migrations/             # EF Core migrations
│   │   ├── Program.cs              # App configuration
│   │   └── primus-portal.db        # SQLite database
│   │
│   └── frontend/                   # React + Vite app
│       ├── src/
│       │   ├── components/         # React components
│       │   ├── pages/              # Page components
│       │   ├── stores/             # Zustand state management
│       │   ├── types/              # TypeScript types
│       │   └── App.tsx             # Main app component
│       └── package.json
│
├── sdk/
│   ├── dotnet/                     # .NET SDK
│   └── nodejs/                     # Node.js SDK
│
├── examples/                       # Example integrations
├── test-apps/                      # Test applications
└── docs/                           # Documentation
```

## API Endpoints

### Authentication
- `POST /api/auth/login` - Login
- `POST /api/auth/refresh` - Refresh token

### Applications
- `GET /api/applications` - List all apps
- `GET /api/applications/{id}` - Get app details
- `POST /api/applications` - Create app
- `PUT /api/applications/{id}` - Update app
- `DELETE /api/applications/{id}` - Delete app
- `POST /api/applications/{id}/regenerate-key` - Regenerate API key

### Modules
- `GET /api/modules` - List all modules
- `POST /api/modules` - Create module
- `PUT /api/modules/{id}` - Update module
- `DELETE /api/modules/{id}` - Delete module

### Documentation
- `GET /api/documentation/{appId}` - Get app documentation

### Upgrade
- `GET /api/upgrade/check/{appId}` - Check for updates

## Configuration

### Backend Configuration

**appsettings.Development.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=primus-portal.db"
  },
  "Jwt": {
    "Secret": "YourSecretKeyHere_MinimumLength32Characters",
    "Issuer": "PrimusSaaS",
    "Audience": "PrimusPortal",
    "ExpirationMinutes": 60
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Frontend Configuration

**Environment Variables** (`.env.development`):
```env
VITE_API_URL=http://localhost:5241
```

**Create the file:**
```bash
cd portal/frontend
echo "VITE_API_URL=http://localhost:5241" > .env.development
```

## Testing

### Backend Tests
```bash
cd sdk/dotnet/PrimusSaaS.Identity.Validator.Tests
dotnet test --logger "console;verbosity=detailed"
```

### Frontend Tests
```bash
cd portal/frontend
npm test
```

### SDK Tests

**.NET SDK:**
```bash
cd sdk/dotnet/PrimusSaaS.Identity.Validator.Tests
dotnet test
```

**Node.js SDK:**
```bash
cd sdk/nodejs/primus-identity-validator
npm test
```

## Troubleshooting

### Backend Issues

**Port already in use:**
```bash
# Find process using port 5241
netstat -ano | findstr :5241

# Kill the process
taskkill /PID <PID> /F
```

**Database locked error:**
- Close any SQLite browser connections
- Restart the backend

**Migration errors:**
```bash
# Remove all migrations and start fresh
rm -r Migrations/
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Frontend Issues

**Port 5173 in use:**
```bash
# Vite will automatically use next available port
# Or specify a different port in vite.config.ts
```

**Module not found:**
```bash
# Clear node_modules and reinstall
rm -r node_modules package-lock.json
npm install
```

**API connection refused:**
- Verify backend is running on port 5241
- Check VITE_API_URL in .env.development
- Check browser console for CORS errors

### CORS Issues

If you see CORS errors, verify `Program.cs`:
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});
```

## Features Available Locally

✅ **Fully Functional:**
- User authentication (admin portal)
- Application registration
- Module management
- API key generation
- Documentation viewer
- SDK integration testing

❌ **Limited (Azure-only):**
- Application Insights monitoring
- Key Vault secret management
- Auto-scaling
- Geographic distribution
- Enterprise SSO (Azure AD)

## Performance Tips

### Backend
- Use `dotnet watch run` for auto-reload
- Enable hot reload: `dotnet watch run --no-hot-reload false`
- Profile with: `dotnet-trace collect`

### Frontend
- Vite HMR (Hot Module Replacement) is enabled by default
- Use React DevTools for debugging
- Enable source maps in production build if needed

### Database
- SQLite is fast for local dev
- Add indexes if queries are slow:
  ```sql
  CREATE INDEX idx_applications_name ON Applications(Name);
  ```

## Upgrading to Azure Later

When you're ready to deploy to Azure:

1. **Update connection string** in `appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=tcp:your-server.database.windows.net,1433;Database=PrimusSaasDb;..."
   }
   ```

2. **Run migrations** against Azure SQL:
   ```bash
   dotnet ef database update
   ```

3. **Use deployment script**:
   ```bash
   .\deploy-azure.ps1 -Environment prod
   ```

4. **Configure GitHub secrets** and push to trigger CI/CD

See `docs/DEPLOYMENT.md` for full Azure deployment guide.

## Best Practices

### Code Organization
- Keep controllers thin, logic in services
- Use DTOs for API requests/responses
- Validate input with data annotations
- Handle errors with middleware

### Database
- Always create migrations for schema changes
- Test migrations on a copy before production
- Keep migrations small and focused
- Document breaking changes

### Git Workflow
- Create feature branches: `git checkout -b feature/my-feature`
- Commit often with clear messages
- Run tests before pushing
- Keep dev-9 branch up to date

### Security
- Never commit secrets to git
- Use environment variables for config
- Rotate JWT secret regularly
- Validate all user input

## Additional Resources

- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core/)
- [React Documentation](https://react.dev/)
- [Vite Documentation](https://vitejs.dev/)
- [Entity Framework Core](https://docs.microsoft.com/ef/core/)
- [Primus SaaS Architecture](./ARCHITECTURE.md)
- [API Documentation](./FAQ.md)

## Getting Help

1. Check existing documentation in `docs/`
2. Review GitHub issues
3. Check application logs
4. Use debugger in VS Code

## Next Steps

- ✅ Run backend and frontend locally
- ✅ Test all portal features
- ✅ Integrate SDKs in test apps
- ✅ Develop new features locally
- ⏳ Deploy to Azure when ready (quota approved)

---

**Happy Coding! 🚀**
