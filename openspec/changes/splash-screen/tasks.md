## 1. Definition

- [ ] 1.1 Settle the open questions in the design (autostart behavior, minimum time, setting to disable, artwork crop) and update the specs accordingly

## 2. Splash window

- [ ] 2.1 Add the splash window with the artwork as an app asset
- [ ] 2.2 Show it first on launch and close it when the main window is ready or startup fails
- [ ] 2.3 Skip it for second instances and, if decided, for hidden autostart

## 3. Verification

- [ ] 3.1 Build and run the tests
- [ ] 3.2 Check manually on macOS and Windows, including the MSIX install and a slow start (deferred to the maintainer)
- [ ] 3.3 Move the `splash-screen` backlog item out of `AGENTS.md`
