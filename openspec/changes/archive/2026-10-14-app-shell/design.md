## Context

Empty repository. Development happens on macOS and the app must build and run there. Windows is the priority platform; Linux if easy. See `proposal.md` for motivation.

## Goals / Non-Goals

**Goals:**
- Solution structure that keeps the core independent from the UI.
- Tray and background lifecycle in a single process.
- A single source of truth for the version.

**Non-Goals:**
- A system service separate from the UI.
- Native OS notifications.
- Packaging and installers.

## Decisions

- **Structure**: `src/MothEidol.Core` (no UI references), `src/MothEidol.App` (Avalonia), `tests/MothEidol.Core.Tests` (xUnit). One `.sln` at the root. Allows testing the core without a UI.
- **UI**: Avalonia with its built-in `TrayIcon`. Rejected: MAUI, no official Linux support.
- **Background**: same process. Closing the window hides it (`Hide`) and exit only happens via "Quit". Rejected: separate service, too costly for the MVP.
- **Single instance**: per-user named mutex plus a local channel to notify the existing instance.
- **Version**: `Directory.Build.props` defines `Version`; the app reads it from assembly metadata. A version bump touches a single file.
- **Start with session**: per-platform implementation behind an interface. Windows first (user `Run` key); Linux (`.desktop` in autostart) and macOS (LaunchAgent) are best effort.

## Risks / Trade-offs

- [The tray icon behaves differently on Linux depending on the desktop environment] → document the limitation and degrade to a minimized window.
- [Start with session is not available on every platform from day one] → disable the option with an explanation (per the spec).
