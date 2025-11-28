# Observability quickstart

## Enable telemetry (optional)
- Leave telemetry off by default (connection string empty).  
- To enable Application Insights, set the env var before `dotnet run`:
  - PowerShell: ``$env:Telemetry__ApplicationInsightsConnectionString="InstrumentationKey=YOUR_KEY"``  
  - Bash: ``export Telemetry__ApplicationInsightsConnectionString="InstrumentationKey=YOUR_KEY"``

## Quick checks
1) Start the API:  
   ```pwsh
   cd portal/backend
   dotnet run
   ```
2) Get a dev token (defaults from `appsettings.Development.json`):  
   ```pwsh
   $body = @{ email="admin@primussaas.local"; password="Admin123!" } | ConvertTo-Json
   $token = (Invoke-RestMethod -Method Post -Uri http://localhost:5000/api/auth/login -ContentType "application/json" -Body $body).token
   ```
3) Hit an API to generate logs:  
   ```pwsh
   Invoke-RestMethod -Headers @{Authorization="Bearer $token"} -Uri http://localhost:5000/api/modules
   ```
4) Check logging metrics endpoint (requires auth):  
   ```pwsh
   Invoke-RestMethod -Headers @{Authorization="Bearer $token"} -Uri http://localhost:5000/primus/logging/metrics
   ```
5) Inspect logs: console output plus rolling files under `portal/backend/logs/`.

## Alert templates (Azure Monitor via Log Alerts)
- File: `portal/backend/alerts/application-insights-alerts.json`
- Contains two scheduled query (log) alerts you can deploy/paste:
  - 5xx error rate > 1% over 5 minutes
  - p95 request duration > 1s over 5 minutes
- Fill in your subscription/resource group/action group IDs and deploy via ARM/Bicep or import into the Azure Portal.***
