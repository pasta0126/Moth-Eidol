## Context

Native notifications are out of the MVP: the dev machine is macOS, which needs a signed app bundle for the native API, and the Windows backend could not be tested locally. See `proposal.md`. Depends on `project-alerts` (throttled `AlertEvent` stream) and `app-shell` (tray).

## Goals / Non-Goals

**Goals:**
- Identical behavior on Windows, macOS and Linux using only Avalonia and the app's own resources.
- A channel abstraction that later accepts native and remote channels.

**Non-Goals:**
- Native OS notifications (Windows toast, macOS `UNUserNotificationCenter`, Linux D-Bus).
- Remote or team-wide channels (webhook, ntfy, email, run command).
- Per-project routing to different channels.

## Decisions

- **Abstraction**: `INotifier.NotifyAsync(AlertEvent)` in `Core`; a dispatcher fans out to enabled notifiers in parallel and isolates failures.
- **Toast**: a small borderless, topmost Avalonia window anchored to a screen corner, stacked with a cap on simultaneous toasts. Rejected: native toasts (see Context).
- **Tray state/counter**: swap the tray icon and tooltip; a numeric badge is drawn on the icon image where the platform allows it.
- **Sound**: behind `IAudioPlayer`. Decided at apply time: the platform's own alert sound (`afplay` on macOS, `MessageBeep` on Windows, the freedesktop sound theme on Linux), so no audio package and no bundled file are needed. A failure is reported by the dispatcher against the sound channel. The channel is off by default: an unexpected beep is more annoying than a missing one.
- **Configuration**: one boolean setting per channel in `local-persistence`, with a default per channel (toast and tray on, sound off); the settings screen is built from the registered channels, so a new channel appears without changing it.

## Risks / Trade-offs

- [Topmost windows behave differently across window managers on Linux] → best effort; document known limits.
- [Cross-platform audio playback needs a dependency] → keep behind an interface; sound is optional.
- [Toast spam if throttling is disabled] → cap simultaneous toasts and collapse overflow into a "+N more" toast.
