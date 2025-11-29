using System;
using System.Collections.Generic;
using System.Linq;

namespace PrimusSaaS.Notifications.Core;

public enum ChannelDispatchStatus
{
    Sent,
    Skipped,
    Failed
}

public sealed record ChannelDispatchResult(
    string Channel,
    ChannelDispatchStatus Status,
    string? Detail = null,
    Exception? Exception = null)
{
    public static ChannelDispatchResult Sent(string channel) =>
        new(channel, ChannelDispatchStatus.Sent);

    public static ChannelDispatchResult Skipped(string channel, string detail) =>
        new(channel, ChannelDispatchStatus.Skipped, detail);

    public static ChannelDispatchResult Failed(string channel, Exception exception) =>
        new(channel, ChannelDispatchStatus.Failed, exception.Message, exception);
}

public sealed record NotificationResult
{
    public bool Success { get; init; }
    public string? ChannelUsed { get; init; }
    public string? FailureReason { get; init; }
    public IReadOnlyCollection<ChannelDispatchResult> Channels { get; init; } =
        Array.Empty<ChannelDispatchResult>();

    public static NotificationResult FromChannels(
        IEnumerable<ChannelDispatchResult> channels,
        string? failureReason = null)
    {
        var channelList = channels.ToArray();
        var channelUsed = channelList.FirstOrDefault(c => c.Status == ChannelDispatchStatus.Sent)?.Channel;
        var success = channelUsed != null;

        return new NotificationResult
        {
            Success = success,
            ChannelUsed = channelUsed,
            FailureReason = success
                ? null
                : failureReason ?? "Notification did not reach any channel.",
            Channels = channelList
        };
    }
}

public sealed class NotificationFailedException : Exception
{
    public NotificationFailedException(string message, NotificationResult result, Exception? innerException = null)
        : base(message, innerException)
    {
        Result = result;
    }

    public NotificationResult Result { get; }
}
