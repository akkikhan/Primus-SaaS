using PrimusSaaS.Notifications.Configuration;

namespace PrimusSaaS.Notifications.Abstractions;

public interface IRateLimiter
{
    bool TryConsume(Recipient recipient, RateLimitOptions options, out string reason);
}
