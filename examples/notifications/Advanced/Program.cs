using PrimusSaaS.Notifications;
using PrimusSaaS.Notifications.Abstractions;

var builder = WebApplication.CreateBuilder(args);
var templatesRoot = Path.Combine(builder.Environment.ContentRootPath, "NotificationTemplates");

builder.Services.AddPrimusNotifications(n =>
{
    n.UseFileTemplates(templatesRoot, validateOnStartup: true, watchForChanges: builder.Environment.IsDevelopment());
    n.UseLogger();
    n.UseInMemoryQueue(o =>
    {
        o.BoundedCapacity = 500;
        o.MaxParallelHandlers = 2;
        o.BaseRetryDelayMs = 250;
    });
    n.UseSmtp(opts => builder.Configuration.GetSection("Notifications:Smtp").Bind(opts));
    n.UseTwilio(opts => builder.Configuration.GetSection("Notifications:Twilio").Bind(opts));
    n.ConfigureDispatch(o =>
    {
        o.ThrowOnFailure = true;
        o.FallbackToLogger = false;
        o.QueueOnFailure = false;
    });
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

app.MapPost("/notify/sms", async (INotificationService notifications, string number) =>
{
    await notifications.SendSmsAsync(number, "Your verification code is 123456");
    return Results.Ok(new { sent = true, to = number });
});

app.Run();
