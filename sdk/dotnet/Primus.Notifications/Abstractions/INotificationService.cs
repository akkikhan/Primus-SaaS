namespace PrimusSaaS.Notifications.Abstractions;

/// <summary>
/// Defines the contract for sending notifications through various channels (Email, SMS, etc.).
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends an email notification directly (without using templates).
    /// </summary>
    /// <param name="to">The recipient email address.</param>
    /// <param name="subject">The email subject.</param>
    /// <param name="body">The email body (HTML supported).</param>
    /// <param name="name">Optional recipient name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Core.NotificationResult"/> indicating success or failure with channel-level details.</returns>
    Task<Core.NotificationResult> SendEmailAsync(
        string to,
        string subject,
        string body,
        string? name = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an SMS notification directly.
    /// </summary>
    /// <param name="phoneNumber">The recipient phone number (E.164 format recommended).</param>
    /// <param name="message">The SMS message content.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Core.NotificationResult"/> indicating success or failure with channel-level details.</returns>
    Task<Core.NotificationResult> SendSmsAsync(
        string phoneNumber,
        string message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification using the specified notification object.
    /// This is the low-level method that supports templates and custom notification types.
    /// </summary>
    /// <param name="notification">The notification to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="fromQueue">Indicates whether this send is being executed by the background queue (prevents re-queue loops).</param>
    /// <returns>A <see cref="Core.NotificationResult"/> indicating success or failure with channel-level details.</returns>
    Task<Core.NotificationResult> SendAsync(INotification notification, CancellationToken cancellationToken = default, bool fromQueue = false);

    /// <summary>
    /// Sends multiple notifications, optionally enqueueing them for background delivery.
    /// </summary>
    /// <param name="notifications">Notifications to process.</param>
    /// <param name="enqueueOnly">When true, enqueue instead of immediate send (if a queue is configured).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyCollection<Core.NotificationResult>> SendBulkAsync(
        IEnumerable<INotification> notifications,
        bool enqueueOnly = false,
        CancellationToken cancellationToken = default);
}
