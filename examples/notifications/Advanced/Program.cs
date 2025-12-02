using PrimusSaaS.Notifications;
using PrimusSaaS.Notifications.Abstractions;
using PrimusSaaS.Notifications.Core;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
var templatesRoot = Path.Combine(builder.Environment.ContentRootPath, "NotificationTemplates");

// Basic config validation / normalization
var smtpSection = config.GetSection("Notifications:Smtp");
var twilioSection = config.GetSection("Notifications:Twilio");
var twilioFrom = NormalizePhone(twilioSection["FromNumber"]);
var templatesExist = Directory.Exists(templatesRoot);

builder.Services.AddPrimusNotifications(n =>
{
    if (!templatesExist)
    {
        throw new InvalidOperationException($"NotificationTemplates folder not found at '{templatesRoot}'. Create it or point UseFileTemplates to an existing path.");
    }

    n.UseFileTemplates(templatesRoot, validateOnStartup: true, watchForChanges: builder.Environment.IsDevelopment());
    n.UseLogger();
    n.UseInMemoryQueue(o =>
    {
        o.BoundedCapacity = 500;
        o.MaxParallelHandlers = 2;
        o.BaseRetryDelayMs = 250;
    });
    n.UseSmtp(opts =>
    {
        smtpSection.Bind(opts);
        if (IsPlaceholder(opts.Host))
        {
            throw new InvalidOperationException("SMTP host is a placeholder; configure a real host or remove SMTP.");
        }
    });
    n.UseTwilio(opts =>
    {
        twilioSection.Bind(opts);
        opts.FromNumber = twilioFrom;
        if (string.IsNullOrWhiteSpace(opts.AccountSid) || string.IsNullOrWhiteSpace(opts.AuthToken))
        {
            throw new InvalidOperationException("Twilio credentials are missing. Set AccountSid/AuthToken or remove Twilio.");
        }
        if (string.IsNullOrWhiteSpace(opts.FromNumber) || !opts.FromNumber.StartsWith("+") || opts.FromNumber.Length < 8)
        {
            throw new InvalidOperationException("Twilio FromNumber must be E.164 (e.g., +16205538468).");
        }
    });
    n.ConfigureDispatch(o =>
    {
        o.ThrowOnFailure = true;
        o.FallbackToLogger = true; // allow dev fallback but surface mode in responses
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

app.MapGet("/primus/notifications/health", () =>
{
    var smtpHost = smtpSection["Host"];
    var smtpConfigured = !IsPlaceholder(smtpHost) && !string.IsNullOrWhiteSpace(smtpHost);
    var twilioConfigured = !string.IsNullOrWhiteSpace(twilioSection["AccountSid"]) && !string.IsNullOrWhiteSpace(twilioSection["AuthToken"]);

    return Results.Ok(new
    {
        templates = new { path = templatesRoot, exists = templatesExist },
        smtp = new { configured = smtpConfigured, host = smtpHost },
        twilio = new { configured = twilioConfigured, from = twilioFrom },
        status = "ok"
    });
});

app.MapPost("/notify/welcome", async (INotificationService notifications, string email) =>
{
    var result = await notifications.SendEmailAsync(email, "Welcome!", $"Thanks for joining, {email}");
    return ToHttpResult(result, new { to = email });
});

app.MapPost("/notify/sms", async (INotificationService notifications, string number) =>
{
    var result = await notifications.SendSmsAsync(number, "Your verification code is 123456");
    return ToHttpResult(result, new { to = number });
});

app.Run();

static bool IsPlaceholder(string? value) =>
    string.IsNullOrWhiteSpace(value) || value.Contains("example", StringComparison.OrdinalIgnoreCase);

static string NormalizePhone(string? raw)
{
    if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
    var digits = new string(raw.Where(char.IsDigit).ToArray());
    if (string.IsNullOrEmpty(digits)) return string.Empty;
    return digits.StartsWith("+" ) ? digits : $"+{digits}";
}

static IResult ToHttpResult(NotificationResult result, object extra)
{
    var mode = result.ChannelUsed ?? result.Channels.FirstOrDefault()?.Channel ?? "logged";
    var response = new
    {
        sent = result.Success,
        mode,
        enqueued = result.EnqueuedForRetry,
        serviceUnavailable = result.ServiceUnavailable,
        failure = result.FailureReason,
        channels = result.Channels,
        extra
    };

    if (result.Success)
    {
        return Results.Ok(response);
    }

    // Surface rate-limit or other errors as 429/503 when hinted
    if (result.ServiceUnavailable || result.Channels.Any(c => c.Detail?.Contains("rate", StringComparison.OrdinalIgnoreCase) == true))
    {
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable, response);
    }

    return Results.BadRequest(response);
}
