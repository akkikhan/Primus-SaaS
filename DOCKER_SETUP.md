# Docker Quick Start Guide

This guide will help you run the entire Primus SaaS Portal using Docker.

## Prerequisites

- Docker Desktop (Windows/Mac) or Docker Engine + Docker Compose (Linux)
- At least 4GB of available RAM

## Quick Start

1. **Copy environment variables**:
   ```bash
   cp .env.example .env
   ```

2. **Start all services**:
   ```bash
   docker-compose up -d
   ```

3. **Wait for services to be ready** (~30 seconds):
   ```bash
   docker-compose logs -f
   ```

4. **Access the portal**:
   - Frontend: http://localhost:5173
   - Backend API: http://localhost:5267
   - Default login: `admin@primussaas.com` / `Admin123!`

## Services

The stack includes:
- **SQL Server** (port 1433) - Database
- **Backend API** (port 5267) - .NET 8 Web API
- **Frontend** (port 5173) - React SPA served by nginx

## Useful Commands

```bash
# View logs
docker-compose logs -f

# Stop all services
docker-compose down

# Stop and remove volumes (reset database)
docker-compose down -v

# Rebuild after code changes
docker-compose up -d --build

# Run database migrations manually
docker-compose exec backend dotnet ef database update
```

## Configuration

Edit `.env` to customize:
- `DB_PASSWORD` - SQL Server password
- `JWT_SECRET` - JWT signing key
- `AZURE_AD_*` - Azure AD configuration (optional)

## Troubleshooting

**Backend won't start**: 
- Check if DB is healthy: `docker-compose ps`
- View logs: `docker-compose logs db backend`

**Frontend can't connect to backend**:
- Ensure `VITE_API_BASE_URL` matches your backend URL
- Rebuild frontend: `docker-compose up -d --build frontend`

**Database connection errors**:
- Wait for DB health check to pass
- Verify password in `.env` matches in both services
