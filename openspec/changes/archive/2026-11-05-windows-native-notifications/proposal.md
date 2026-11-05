## Why

On Windows the alerts are only shown as in-app toasts, which look foreign to the OS, do not reach the Action Center and ignore Focus Assist / Do Not Disturb. Users expect native Windows notifications, and need to choose whether an alert makes a sound or not.

## What Changes

- On Windows, each alert is delivered as a native Windows notification (toast with the Action Center history) showing the project, the level and the entry headline; clicking it opens the application.
- On Windows the native notification replaces the in-app toast by default; the in-app toast remains available as a channel and is used as a fallback when the native notification cannot be shown.
- The alert sound becomes a persisted application setting, "Alert sound", with two values: with sound or silent. On Windows it is applied to the native notification itself (no second beep); on macOS and Linux it keeps driving the existing sound channel.
- Default on Windows: native notification on, sound on. Other platforms keep today's defaults (in-app toast on, sound off).
- No new dependencies: the Windows SDK projection already referenced by the Windows build is used.

## Capabilities

### New Capabilities

(none)

### Modified Capabilities
- `notification-channels`: adds the native Windows notification channel, the Windows default channel set with in-app toast fallback, and the sound setting semantics.

## Impact

- Core: new settings accessor for the sound preference; channel defaults become platform-aware through the notifier abstraction.
- App: new `WindowsNotifier` (Windows only, behind the `INotifier` abstraction), AUMID registration for the unpackaged build, sound switch in application settings, fallback to the toast.
- Packaging: MSIX build uses the package identity; zip build registers an app identity under the current user (no admin).
- Cannot be fully verified on the macOS development machine; manual check on Windows is deferred to the maintainer.
