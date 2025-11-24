# Testing Guide

This guide covers how to test applications using `PrimusSaaS.Identity.Validator`, including unit testing, integration testing, and local development.

---

## 1. Unit Testing

Since `PrimusSaaS` relies on `HttpContext` and `ClaimsPrincipal`, you can easily mock these for unit tests.

### Mocking Tenant Context

If you use the `ITenantService` pattern (see [INTEGRATION_PATTERNS.md](./INTEGRATION_PATTERNS.md)), you can mock the interface directly.

```csharp
[Fact]
public async Task GetOrders_ShouldFilterByTenant()
{
    // Arrange
    var mockTenantService = new Mock<ITenantService>();
    mockTenantService.Setup(x => x.GetTenantId()).Returns("tenant-123");
    
    var service = new OrderService(mockTenantService.Object, mockRepo.Object);
    
    // Act
    await service.GetOrdersAsync();
    
    // Assert
    mockRepo.Verify(x => x.GetOrdersByTenantAsync("tenant-123"), Times.Once);
}
```

### Mocking HttpContext Extensions

If you use the extension methods (`GetTenantId`, `Get`), you need to construct a mock `HttpContext`.

```csharp
[Fact]
public void Controller_ShouldReturnCurrentTenant()
{
    // Arrange
    var controller = new OrdersController();
    var httpContext = new DefaultHttpContext();
    
    // Setup User Claims
    var claims = new[] { new Claim("sub", "user-1") };
    var identity = new ClaimsIdentity(claims, "TestAuth");
    httpContext.User = new ClaimsPrincipal(identity);
    
    // Setup Tenant Context
    var tenantContext = new TenantContext { TenantId = "tenant-123" };
    httpContext.Items["TenantContext"] = tenantContext;
    
    controller.ControllerContext = new ControllerContext 
    { 
        HttpContext = httpContext 
    };
    
    // Act
    var result = controller.GetOrders();
    
    // Assert
    // ... verify result contains tenant-123
}
```

---

## 2. Integration Testing

For integration tests using `WebApplicationFactory`, you can bypass authentication or use a test token.

### Bypassing Auth (Test Scheme)

Register a custom authentication scheme for tests.

```csharp
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
        });
    }
}

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] 
        { 
            new Claim("sub", "test-user"), 
            new Claim("tid", "test-tenant") 
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");
        
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
```

### Using Local Dev Tokens

You can also use the "LocalAuth" issuer configuration and generate real JWTs signed with the dev secret. See [LOCAL_DEVELOPMENT_GUIDE.md](./LOCAL_DEVELOPMENT_GUIDE.md) for details.

---

## 3. Local Development

See [LOCAL_DEVELOPMENT_GUIDE.md](./LOCAL_DEVELOPMENT_GUIDE.md) for a complete guide on setting up your environment for offline development without Azure AD.

---

## 4. Postman Testing

We provide a Postman collection to help you test your integration.

1. Import `PrimusSaaS.Identity.Validator.postman_collection.json`
2. Configure your environment variables (BaseUrl, Token)
3. Use the "Generate Local Token" request (if you implemented the dev controller)
4. Call your protected endpoints
