## 1. Definition

- [x] 1.1 Settle the open questions in the design (autostart behavior, minimum time, setting to disable, artwork crop) and update the specs accordingly

## 2. Splash window

- [x] 2.1 Add the splash window with the artwork as an app asset
- [x] 2.2 Show it first on launch and close it when the main window is ready or startup fails
- [x] 2.3 Skip it for second instances (the application never starts hidden today, so nothing else to skip)
- [x] 2.4 Setting "Show the splash screen at startup" (on by default) with its unit tests, and the 2-second minimum display as a tested helper

## 3. Verification

- [x] 3.1 Build and run the tests
- [ ] 3.2 Check manually on macOS and Windows, including the MSIX install and a slow start (deferred to the maintainer)
- [x] 3.3 Move the `splash-screen` backlog item out of `AGENTS.md`
