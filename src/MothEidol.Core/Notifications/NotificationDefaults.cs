namespace MothEidol.Core.Notifications;

/// <summary>
/// Platform-dependent channel defaults. On Windows the native notification replaces the in-app toast and plays
/// the sound itself; elsewhere the in-app toast is on and the sound is opt-in. A stored user choice always wins
/// over these (see <c>AppSettings.IsChannelEnabled</c>).
/// </summary>
public static class NotificationDefaults
{
    public static bool NativeExists(bool windows) => windows;

    public static bool ToastEnabledByDefault(bool windows) => !windows;

    public static bool SoundEnabledByDefault(bool windows) => windows;
}
