namespace Primus.Notifications.Configuration;

public class SmtpOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "Primus Notification";
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetryCount { get; set; } = 2;
    public int RetryBaseDelayMs { get; set; } = 200;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Host))
        {
            throw new ArgumentException("SMTP Host must be configured", nameof(Host));
        }

        if (Port <= 0)
        {
            throw new ArgumentException("SMTP Port must be greater than zero", nameof(Port));
        }

        if (string.IsNullOrWhiteSpace(FromAddress))
        {
            throw new ArgumentException("SMTP FromAddress must be configured", nameof(FromAddress));
        }
    }
}
