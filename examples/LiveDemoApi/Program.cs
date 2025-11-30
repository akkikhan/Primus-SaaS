using PrimusSaaS.Identity.Validator;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// =========================================================================
// DEMO STEP 1: Register Primus Identity Services
// =========================================================================
// This single line binds the configuration from appsettings.json
// and sets up all necessary validation logic for multiple providers.
builder.Services.AddPrimusIdentity(options =>
{
    builder.Configuration.GetSection("PrimusIdentity").Bind(options);
});



// Standard ASP.NET Core Authorization
builder.Services.AddAuthorization();

// Enable CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173") // Vite default port
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

// Add HttpClient for Auth0 proxy
builder.Services.AddHttpClient();

// Enable PII for debugging
Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

app.UseHttpsRedirection();

// =========================================================================
// DEMO STEP 2: Add Middleware
// =========================================================================
// Ensure these are placed between UseHttpsRedirection and MapControllers
app.UseAuthentication();
app.UseAuthorization();

// =========================================================================
// DEMO STEP 3: Diagnostics (Optional)
// =========================================================================
// This endpoint (/primus/diagnostics) allows us to verify our configuration
// and see exactly which keys are loaded from Azure AD and Auth0.
app.MapPrimusIdentityDiagnostics();

// =========================================================================
// DEMO HELPER: Real Token Proxy
// =========================================================================
// 1. Auth0 Proxy (Client Credentials Flow)
app.MapPost("/auth/auth0", async (IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var response = await client.PostAsJsonAsync("https://dev-ft7bykiq2exe4ua4.us.auth0.com/oauth/token", new
    {
        client_id = "h4CjtEYT0HiXwJVr3JkOSkJnr1aq3bHc",
        client_secret = "6Si0dfpi89xei4GGGcxblXIb2dc6r8RpfLqPAqaleN_sy3c6PmSLbrTfDAfm_sLm",
        audience = "https://saas-api/",
        grant_type = "client_credentials"
    });

    var content = await response.Content.ReadAsStringAsync();
    return Results.Content(content, "application/json");
});

// 2. Azure Proxy (CLI Token)
app.MapPost("/auth/azure", async () =>
{
    try
    {
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c az account get-access-token --resource https://management.azure.com/ --query accessToken -o tsv",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        var token = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(token))
            return Results.BadRequest(new { error = $"Azure CLI Error: {error}. Ensure you are logged in with 'az login'." });

        return Results.Ok(new { access_token = token.Trim(), token_type = "Bearer" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi()
.RequireAuthorization(); // <--- DEMO STEP 4: Secure the endpoint

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
