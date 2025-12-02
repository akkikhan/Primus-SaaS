using Microsoft.AspNetCore.Authorization;
using PrimusSaaS.Identity.Validator;
using PrimusSaaS.Identity.Validator.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPrimusIdentity(opts => builder.Configuration.GetSection("PrimusIdentity").Bind(opts));
builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

// Diagnostics endpoint (dev only)
app.MapPrimusIdentityDiagnostics();

app.MapGet("/public", () => "public ok");
app.MapGet("/secure", [Authorize] () => "secure ok").RequireAuthorization();

app.Run();
