## Why

While the application starts there is a short moment with nothing on screen, which feels unfinished. A branded splash screen, in the spirit of Adobe applications, gives immediate feedback that the app is launching and shows the moth identity. This is already in the `AGENTS.md` backlog; the proposal is deliberately light and the details are to be defined when development starts.

## What Changes

- A small borderless splash window with the branded artwork (`assets/branding/moth-eidol-banner.png`) is shown as soon as the application process starts.
- The splash closes when the main window is ready (or when the app starts hidden in the tray, as soon as startup finishes).
- The splash never blocks or delays startup on its own and cannot be left open by an error.
- Open: minimum display time, progress text and whether it can be disabled (see the design).

## Capabilities

### New Capabilities
- `splash-screen`: the startup splash window and when it is shown and dismissed.

### Modified Capabilities

None.

## Impact

- `src/MothEidol.App`: a new splash window and a small change in the startup sequence (`Program.cs`, `App.axaml.cs`).
- The banner image is added as an app asset; check its size to keep the single-file package small.
- No new dependencies.
