## Why

The tray icon is a single fixed image. On a light OS taskbar or menu bar it can be hard to see, and on macOS it does not look native (menu bar icons are expected to be monochrome template images that adapt to light, dark and highlighted states).

## What Changes

- The tray icon uses a monochrome variant that follows the operating system theme: light glyph on dark taskbars/menu bars, dark glyph on light ones.
- On macOS the icon is a template image so the system tints it itself.
- The icon updates live when the OS theme changes, without restarting the app.
- The pending-alerts state and the unread counter stay readable in both variants.
- The tray icon follows the **OS** theme, not the in-app Moth/Vesper theme (that choice stays independent).

## Capabilities

### New Capabilities
_None._

### Modified Capabilities
- `app-shell`: the tray icon adapts to the OS theme.
- `app-theme`: clarifies that the tray icon is independent of the in-app theme but follows the OS theme.

## Impact

- App: `TrayPresenter` and tray assets (monochrome light/dark variants, macOS template image), OS theme change detection behind the existing platform abstraction.
- No new dependencies expected (Avalonia exposes the platform theme variant); to be confirmed in design.
