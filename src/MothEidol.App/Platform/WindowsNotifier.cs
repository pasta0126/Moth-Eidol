#if WINDOWS10_0_19041_0_OR_GREATER
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;
using MothEidol.Core.Alerts;
using MothEidol.Core.Notifications;
using MothEidol.Core.Platform;
using MothEidol.Core.Projects;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using OsToastNotifier = Windows.UI.Notifications.ToastNotifier;

namespace MothEidol.App.Platform;

/// <summary>
/// Delivers each alert as a native Windows notification (banner plus Action Center entry). The sound comes from
/// the notification itself, driven by the "Alert sound" setting. If the notification cannot be shown, the alert
/// goes to the in-app toast instead and the failure is rethrown so the dispatcher records it.
/// </summary>
[SupportedOSPlatform("windows10.0.19041.0")]
public sealed class WindowsNotifier(
    ProjectService projects,
    Func<bool> soundEnabled,
    Action activateApp,
    Action<AlertDelivery> fallback) : INotifier
{
    /// <summary>AppUserModelID of the unpackaged build; the MSIX build uses its package identity instead.</summary>
    public const string Aumid = "MothEidol.App";

    private OsToastNotifier? _notifier;

    public string Id => ChannelIds.Native;

    public string DisplayName => "Windows notification";

    public bool EnabledByDefault => true;

    public Task NotifyAsync(AlertDelivery delivery, CancellationToken cancellationToken)
    {
        try
        {
            var name = projects.Lookup(delivery.ProjectId)?.Name ?? "Unknown project";
            var text = AlertText.For(delivery, name);

            var xml = new XmlDocument();
            xml.LoadXml(ToastXml.Build(text, silent: !soundEnabled()));
            var toast = new ToastNotification(xml);
            toast.Activated += (_, _) => activateApp();

            var notifier = Notifier();
            if (notifier.Setting != NotificationSetting.Enabled)
                throw new InvalidOperationException($"Windows notifications are not enabled for this application ({notifier.Setting}).");

            notifier.Show(toast);
            return Task.CompletedTask;
        }
        catch
        {
            fallback(delivery);
            throw;
        }
    }

    private OsToastNotifier Notifier() => _notifier ??= PackageIdentity.IsPackaged
        ? ToastNotificationManager.CreateToastNotifier()
        : ToastNotificationManager.CreateToastNotifier(Aumid);

    /// <summary>
    /// Registers the application identity the unpackaged build needs for its notifications (display name and
    /// icon) under the current user, and sets it on the process. No administrator rights are needed. Best effort:
    /// when it fails the first notification fails too and the in-app toast takes over.
    /// </summary>
    public static void RegisterIdentity(string? iconPath)
    {
        if (PackageIdentity.IsPackaged) return;

        try
        {
            using var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\AppUserModelId\{Aumid}");
            key.SetValue("DisplayName", "Moth-Eidol");
            if (iconPath is not null) key.SetValue("IconUri", iconPath);
            SetCurrentProcessExplicitAppUserModelID(Aumid);
        }
        catch (Exception)
        {
            // See the summary.
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SetCurrentProcessExplicitAppUserModelID(string appId);
}
#endif
