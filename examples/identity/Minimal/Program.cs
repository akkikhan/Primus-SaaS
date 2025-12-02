using Microsoft.AspNetCore.Authorization;
using PrimusSaaS.Identity.Validator;

var builder = WebApplication.CreateBuilder(args);

// Identity + auth
builder.Services.AddPrimusIdentity(opts => builder.Configuration.GetSection("PrimusIdentity").Bind(opts));
builder.Services.AddAuthorization();

// Swagger
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

app.MapGet("/public", () => "public ok");
app.MapGet("/secure", [Authorize] () => "secure ok").RequireAuthorization();

app.Run();
