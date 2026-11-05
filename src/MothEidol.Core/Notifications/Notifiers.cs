using MothEidol.Core.Alerts;
using MothEidol.Core.Projects;

namespace MothEidol.Core.Notifications;

/// <summary>Plays a short sound. Implemented per platform outside the core.</summary>
public interface IAudioPlayer
{
    Task PlayAlertAsync(CancellationToken cancellationToken);
}

/// <summary>Shows each alert as an in-app toast, even when the main window is hidden in the tray.</summary>
public sealed class ToastNotifier(
    ToastStack stack, ProjectService projects, TimeProvider? time = null, bool enabledByDefault = true) : INotifier
{
    public string Id => ChannelIds.Toast;

    public string DisplayName => "In-app toast";

    public bool EnabledByDefault => enabledByDefault;

    /// <summary>Adds the alert to the stack directly, bypassing the enabled check (used as a fallback).</summary>
    public void Show(AlertDelivery delivery) => NotifyAsync(delivery, CancellationToken.None).GetAwaiter().GetResult();

    public Task NotifyAsync(AlertDelivery delivery, CancellationToken cancellationToken)
    {
        var name = projects.Lookup(delivery.ProjectId)?.Name ?? "Unknown project";
        var text = AlertText.For(delivery, name);
        stack.Add(text.ProjectName, text.Level, text.Headline, text.Detail, (time ?? TimeProvider.System).GetUtcNow());
        return Task.CompletedTask;
    }
}

/// <summary>Counts the alerts the user has not acknowledged yet, for the tray icon.</summary>
public sealed class TrayNotifier(UnreadCounter counter) : INotifier
{
    public string Id => ChannelIds.Tray;

    public string DisplayName => "Tray icon alert state and counter";

    public bool EnabledByDefault => true;

    public Task NotifyAsync(AlertDelivery delivery, CancellationToken cancellationToken)
    {
        counter.Add(delivery);
        return Task.CompletedTask;
    }
}

/// <summary>Plays a sound once for each delivery.</summary>
public sealed class SoundNotifier(IAudioPlayer audio) : INotifier
{
    public string Id => ChannelIds.Sound;

    public string DisplayName => "Sound";

    // Opt-in: an unexpected beep is more annoying than a missing one.
    public bool EnabledByDefault => false;

    public Task NotifyAsync(AlertDelivery delivery, CancellationToken cancellationToken) =>
        audio.PlayAlertAsync(cancellationToken);
}
