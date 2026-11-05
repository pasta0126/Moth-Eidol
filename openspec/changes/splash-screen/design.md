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

## Open questions

- Is the splash shown when the app starts hidden to the tray by autostart? Showing it at every login may be annoying; a likely answer is to show it only on a manual launch.
- Minimum display time (for example 1 second) so it does not flash, or close as soon as the main window is ready?
- Should it be possible to disable the splash in the application settings?
- Does the banner need a splash-specific crop or size? The current artwork is a banner.
- How does it interact with the single-instance rule (a second launch should not show a splash)?

## Risks / Trade-offs

- A splash that outlives a failed startup would leave a stuck window; it must close on any startup error.
- A large image increases the package size slightly.
