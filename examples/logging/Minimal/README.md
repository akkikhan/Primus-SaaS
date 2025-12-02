# Logging Minimal Example

Primus Logging with console output, PII masking, and request logging.

## Run
```bash
dotnet restore
dotnet run
```

## Test
- GET `http://localhost:5000/ping`
- Swagger UI: `http://localhost:5000/swagger`

Logs are structured and include correlation/request IDs.
