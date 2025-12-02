using PrimusSaaS.Logging.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddPrimus(opts => builder.Configuration.GetSection("PrimusLogging").Bind(opts));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UsePrimusLogging();

app.MapGet("/ping", () => Results.Ok(new { message = "pong" }));

app.Run();
