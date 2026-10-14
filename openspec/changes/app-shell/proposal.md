## Why

The project has no code yet. Before adding monitoring we need a runnable cross-platform application that lives in the tray and in the background, so every later change can be tested manually. This is version `0.1.0`.

## What Changes

- Create the .NET solution with a core / app / tests structure.
- Avalonia application with a main window showing the version.
- Tray icon with a menu (Open, Quit).
- Closing the window hides it to the tray; the process keeps running.
- Single instance per user.
- Start-with-session option (off by default).
- Semantic versioning with a single source of truth.

## Capabilities

### New Capabilities
- `app-shell`: main window, system tray, background execution, single instance, start with session and visible version.

### Modified Capabilities

## Impact

- New code: `src/MothEidol.App`, `src/MothEidol.Core`, `tests/`.
- New dependencies: Avalonia, xUnit.
- Establishes the versioning policy and release flow (see `AGENTS.md`).
