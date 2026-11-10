## Context

The artwork `assets/branding/moth-eidol-banner.png` already exists. The app is a single-instance tray application that can start hidden (autostart) and shows the version only in the main window footer (the version must not appear on images or in other windows). Details are intentionally left open until development.

## Goals / Non-Goals

**Goals:**
- Show branded feedback during startup without making startup slower.
- Behave well on Windows, macOS and Linux, including a Store (MSIX) install.

**Non-Goals:**
- A loading progress bar tied to real work.
- Animated splash or sound.

## Decisions

- The splash is shown by the app itself as the first window, not by an OS-level launch screen. Rationale: it is cross-platform and needs no per-OS packaging.
- The splash never shows the version (consistent with the footer-only version rule).

## Decisions settled with the maintainer

- **Artwork:** `assets/branding/moth-eidol-splash.png` (a glowing moth on obsidian, no text), shrunk to a 1280x853 app asset (the window is 640x427, 3:2) so the package stays small. The name `MOTH-EIDOL` and the tagline `SYSTEM OBSERVABILITY COMPOUND` are drawn by the app over the dark lower third with the theme font, never baked into the image; the version never appears.
- **Always dark:** the splash uses the Moth (dark) colors whatever theme is active, because its artwork is dark.
- **When it is shown:** on every launch that shows the main window. Today the application never starts hidden (autostart opens the main window like a manual launch), so there is nothing to skip; if a "start in the tray" mode is added later, it will skip the splash. A second instance never shows it: the single-instance check runs first and hands over to the running one.
- **How long:** it appears before the rest of the startup work and stays at least 2 seconds, long enough to be seen and so it does not flash; it closes as soon as the main window is ready after that.
- **Setting:** "Show the splash screen at startup" in the application settings, on by default. It is read before the splash is shown, so turning it off takes effect on the next launch.
- **Failures:** the splash closes on any startup error, and it never delays the startup beyond the 2 second minimum.

## Risks / Trade-offs

- A splash that outlives a failed startup would leave a stuck window; it must close on any startup error.
- A large image increases the package size slightly.
