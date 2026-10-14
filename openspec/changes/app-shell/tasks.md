## 1. Solution and version

- [x] 1.1 Create `MothEidol.sln`, `src/MothEidol.Core`, `src/MothEidol.App` and `tests/MothEidol.Core.Tests`
- [x] 1.2 Add `Directory.Build.props` with `Version` 0.1.0 (first release; later bumps are decided at release time)
- [x] 1.3 Fill the "Commands" section of `AGENTS.md` with `dotnet build`, `dotnet test` and `dotnet run`

## 2. Application

- [x] 2.1 Avalonia main window showing the version
- [x] 2.2 Tray icon with "Open" and "Quit" menu
- [x] 2.3 Window close hides to the tray; "Quit" terminates the process
- [x] 2.4 Single instance that restores the existing window

## 3. Start with session

- [x] 3.1 Autostart interface and UI option (off by default)
- [x] 3.2 Windows implementation
- [x] 3.3 Disable the option with an explanation on unsupported platforms

## 4. Verification

- [x] 4.1 Core tests for the version service and autostart (with a test double)
- [ ] 4.2 Build and run on macOS; check startup, tray, background close and quit
- [ ] 4.3 Close the change: archive, bump the version (semver), commit, push and tag it
