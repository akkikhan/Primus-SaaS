using PrimusSaaS.Identity.Validator;

var builder = WebApplication.CreateBuilder(args);

// Add controllers and Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Primus Identity from configuration
builder.Services.AddPrimusIdentity(options =>
{
    builder.Configuration.GetSection("PrimusIdentity").Bind(options);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Optional: diagnostics endpoint for issuers/JWKS/security metrics
app.MapPrimusIdentityDiagnostics(); // GET /primus/diagnostics

// Optional: logging metrics/health (requires Primus logger wiring; shown here as a placeholder)
// app.MapPrimusLoggingMetrics(); // GET /primus/logging/metrics
// app.MapGet("/primus/logging/health", (PrimusSaaS.Logging.Core.Logger logger) =>
// {
//     var metrics = logger.GetMetricsSnapshot();
//     var healthy = metrics.WriteFailures == 0;
//     return Results.Json(new { healthy, metrics });
// });

app.MapControllers();

app.Run();
