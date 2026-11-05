## Context

Channels implement `INotifier` and are registered in `App.axaml.cs`; enablement is stored as `notify.<id>.enabled` and `EnabledByDefault` is a fixed property. The sound channel is a regular channel (off by default) played by `SystemAudioPlayer`. The Windows build already targets `net10.0-windows10.0.19041.0`, so the WinRT `Windows.UI.Notifications` projection is available.

## Decisions

- New `WindowsNotifier` (id `native`) in `src/MothEidol.App/Platform`, registered only when `OperatingSystem.IsWindows()`. It builds the toast XML from `AlertText` (project, level, headline) and calls `ToastNotificationManager.CreateToastNotifier(aumid).Show`. Rationale: no new dependency (AGENTS.md), core stays free of OS code.
- Identity: packaged (MSIX) uses the package identity; the zip build registers an AUMID under `HKCU\Software\Classes\AppUserModelId\<id>` (display name and icon) at startup. No admin needed.
- Sound: one setting `notify.sound.enabled` (reuses the existing key, so stored values carry over). On Windows the toast XML carries `<audio silent="true"/>` when silent, otherwise the default notification sound; the `sound` channel is not registered on Windows to avoid a double beep and its row is replaced by the "Alert sound" switch. On other platforms the switch drives the existing `SoundNotifier`. Defaults: on for Windows, off elsewhere.
- Defaults: `INotifier.EnabledByDefault` is already per-notifier, so the toast notifier receives a constructor flag (`enabledByDefault: !OperatingSystem.IsWindows()`); stored user choices still win.
- Fallback: `WindowsNotifier` takes a fallback callback; on any exception or when the OS reports notifications disabled, it adds the alert to the `ToastStack` and rethrows so the dispatcher records the failure.
- Click: the toast activation event shows and focuses the main window through the existing tray/window abstraction. Activation from the Action Center after the app was closed is out of scope (the app only alerts while running).
- Windows-only code is guarded by `[SupportedOSPlatform("windows")]`; tests cover the XML builder (pure function) and the settings/defaults on any OS.

## Risks

- Cannot be run on the macOS dev machine; verification on Windows is manual (maintainer).
- Focus Assist / Do Not Disturb may suppress banners: the notification still lands in the Action Center, which is the expected OS behavior.
- AUMID registration is per user; if it fails the fallback toast keeps alerts visible.
