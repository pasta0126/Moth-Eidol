using MothEidol.Core.Alerts;

namespace MothEidol.Core.Notifications;

public static class ChannelIds
{
    public const string Toast = "toast";
    public const string Tray = "tray";
    public const string Sound = "sound";
}

/// <summary>
/// A way of telling the user about an alert (toast, tray counter, sound, and later native notifications,
/// webhooks, email...). New channels only implement this and register with the dispatcher.
/// </summary>
public interface INotifier
{
    /// <summary>Stable identifier, used to store whether the channel is enabled.</summary>
    string Id { get; }

    /// <summary>Name shown in the settings.</summary>
    string DisplayName { get; }

    /// <summary>Whether the channel is on unless the user says otherwise.</summary>
    bool EnabledByDefault { get; }

    Task NotifyAsync(AlertDelivery delivery, CancellationToken cancellationToken);
}

public sealed class ChannelFailedEventArgs(string channelId, Exception error) : EventArgs
{
    public string ChannelId { get; } = channelId;

    public Exception Error { get; } = error;
}
