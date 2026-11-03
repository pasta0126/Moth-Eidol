## Context

Sources are a file or a folder plus a pattern following the newest file; paths may be local or SMB shares. OS-specific code must live behind an abstraction.

## Decisions

- Add an `IFileManager`-style abstraction (name decided at implementation) with `Reveal(path)`: macOS `open -R` for files / `open` for folders, Windows `explorer.exe /select,"path"` / folder path, Linux `xdg-open` on the folder. Launched without a shell, path passed as an argument, to avoid injection from odd file names. Rationale: no dependency and testable with a fake.
- Target resolution lives in the core: for a folder source, the newest followed file if known, else the folder; for a file source, that file. Check existence with a short timeout so an offline share does not freeze the UI; failure returns a user-facing message.
- "All sources" in the tail: if all sources share one folder, open it; otherwise show a small menu listing the folders.
- Linux without `xdg-open` reports a message with the path instead of failing.

## Risks

- Network shares can block on existence checks: run the check off the UI thread with a timeout.
