## Context

`TrayPresenter` builds the tray icon and its unread-counter state from fixed assets. The in-app theme (Moth/Vesper) is deliberately unrelated to the tray (see `app-theme`).

## Decisions

- Provide two monochrome assets (light glyph, dark glyph) plus a macOS template asset; the counter/alert state is drawn so it remains legible on both.
- Detect the OS theme through Avalonia's platform settings (`ActualThemeVariant` of the platform) behind an abstraction in `Platform/`, so the core stays UI-independent.
- Re-select the variant on platform theme change events; no polling.
- macOS: use a template image (system tints for light/dark/highlight). The colored alert state must still be distinguishable; if a template cannot carry it, use a badge-style overlay (to validate on the macOS dev machine).
- The in-app theme choice does not affect the tray.

## Risks

- Avalonia `TrayIcon` template-image support on macOS may be limited; fallback is selecting light/dark PNGs by OS theme.
- Windows taskbar theme can differ from the app theme setting (separate "system" vs "apps" light mode); the detection should read the system (taskbar) value, to verify.
- Linux trays vary widely; best effort only.
