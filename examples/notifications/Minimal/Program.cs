using PrimusSaaS.Notifications;
using PrimusSaaS.Notifications.Abstractions;
using PrimusSaaS.Notifications.Core;

var builder = WebApplication.CreateBuilder(args);
var templatesRoot = Path.Combine(builder.Environment.ContentRootPath, "NotificationTemplates");

builder.Services.AddPrimusNotifications(n =>
{
    n.UseLogger(); // dev-safe
    n.UseSmtp(opts => builder.Configuration.GetSection("Notifications:Smtp").Bind(opts));
    n.UseFileTemplates(templatesRoot, validateOnStartup: true);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/notify/welcome", async (INotificationService notifications, string email) =>
{
    await notifications.SendEmailAsync(email, "Welcome!", $"Thanks for joining, {email}");
    return Results.Ok(new { sent = true, to = email });
});

app.Run();
