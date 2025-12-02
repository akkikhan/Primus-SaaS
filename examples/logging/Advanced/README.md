# Logging Advanced Example

Primus Logging with console (JSON), file sink, and optional Application Insights.

## Run
```bash
dotnet restore
dotnet run
```

## Test
- GET `http://localhost:5000/ping`
- Swagger UI: `http://localhost:5000/swagger`

## Configure
- Set `PrimusLogging:ApplicationInsights:ConnectionString` for AI (leave blank to skip).
- Logs go to console and `logs/app.log` with daily rolling.
