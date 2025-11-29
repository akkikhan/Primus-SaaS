using Microsoft.EntityFrameworkCore;
using PrimusSaaS.Logging.Extensions;
using PrimusSaaS.Logging.Core;
using PrimusSaaS.Identity.Validator;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// 1. EF CORE SETUP
// ============================================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("EfCoreCompatTest"));

// ============================================
// 2. PRIMUS LOGGING (from NuGet v1.2.2)
// ============================================
builder.Services.AddPrimusLogging(options =>
{
    options.ApplicationId = "EFCORE-COMPAT-TEST";
    options.Environment = "test";
    options.MinLevel = PrimusSaaS.Logging.Core.LogLevel.Debug;
    options.Targets = new List<TargetConfig>
    {
        new() { Type = "console", Pretty = true }
    };
});

// ============================================
// 3. PRIMUS IDENTITY VALIDATOR (from NuGet v1.3.3)
// ============================================
builder.Services.AddPrimusIdentity(options =>
{
    options.Issuers = new List<IssuerConfig>
    {
        new IssuerConfig
        {
            Name = "auth0",
            Issuer = "https://dev-ft7bykiq2exe4ua4.us.auth0.com/",
            Authority = "https://dev-ft7bykiq2exe4ua4.us.auth0.com/",
            Audiences = new List<string> { "https://saas-api/" },
            Type = IssuerType.Auth0
        }
    };
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();

// Seed test data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Products.AddRange(
        new Product { Id = 1, Name = "Laptop", Price = 999.99m },
        new Product { Id = 2, Name = "Mouse", Price = 29.99m },
        new Product { Id = 3, Name = "Keyboard", Price = 79.99m }
    );
    db.SaveChanges();
}

app.UseAuthentication();
app.UseAuthorization();

// ============================================
// TEST ENDPOINTS
// ============================================

// Test 1: EF Core + Logging integration
app.MapGet("/products", async (AppDbContext db, Logger logger) =>
{
    logger.Info("Fetching products from EF Core InMemory database");
    var products = await db.Products.ToListAsync();
    logger.Info("Fetched products", new Dictionary<string, object?> { ["count"] = products.Count });
    return Results.Ok(products);
});

// Test 2: EF Core CRUD with logging
app.MapPost("/products", async (AppDbContext db, Logger logger, Product product) =>
{
    var timer = logger.StartTimer();
    logger.Info("Creating new product", new Dictionary<string, object?> { ["name"] = product.Name });
    
    db.Products.Add(product);
    await db.SaveChangesAsync();
    
    timer.Done("Product created", new Dictionary<string, object?> { ["id"] = product.Id });
    return Results.Created($"/products/{product.Id}", product);
});

// Test 3: Protected endpoint (Identity Validator)
app.MapGet("/protected", () => Results.Ok(new { message = "You are authenticated!" }))
   .RequireAuthorization();

// Test 4: Public endpoint
app.MapGet("/public", () => Results.Ok(new { message = "Public endpoint - no auth required" }));

// Health check
app.MapGet("/health", () => Results.Ok(new 
{ 
    status = "healthy",
    packages = new[]
    {
        "PrimusSaaS.Logging v1.2.2 (NuGet)",
        "PrimusSaaS.Identity.Validator v1.3.3 (NuGet)",
        "Microsoft.EntityFrameworkCore v8.0.10"
    }
}));

app.Run();

// ============================================
// EF CORE ENTITIES
// ============================================
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Product> Products => Set<Product>();
}
