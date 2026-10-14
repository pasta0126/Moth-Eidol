## Why

Alerts must reach the user even when the window is hidden. Native OS notifications are out of the MVP (macOS is the dev machine and the hardest case), so alerts are delivered through app-controlled channels that behave the same on every platform.

## What Changes

- A pluggable chain of notification channels fed by throttled alert events.
- In-app toast channel (always-on-top, corner of the screen).
- Tray icon state and unread-alert counter.
- Sound channel.
- Per-channel enable/disable configuration.
- Extension point for future channels (native OS, webhook, command, etc.).

## Capabilities

### New Capabilities
- `notification-channels`: delivery of alerts through configurable in-app, tray and sound channels.

### Modified Capabilities

## Impact

- New code in `MothEidol.Core` (channel abstraction) and `MothEidol.App` (toast, tray state, sound).
- Consumes the throttled alert stream from `alert-rules`; settings via `local-persistence`.
- Native OS notifications are explicitly not implemented here.
