## 1. Core

- [x] 1.1 Add `IUpdateSource` and `UpdateCheckResult` (`NoUpdate`, `Available(version)`, `Failed(reason)`)
- [x] 1.2 Add `UpdateNoticePolicy`: announce only when enabled, available version > running and ≠ dismissed, comparing `major.minor.patch` (fourth part ignored)
- [x] 1.3 Add `AppSettings.CheckForUpdates` (`update.check_enabled`, default on) and `AppSettings.DismissedUpdateVersion` (`update.dismissed_version`)
- [x] 1.4 Add `UpdateCheckService`: one background check per start with a 30 s timeout, current notice in memory, `NoticeAvailable` event, `Postpone()`, `Dismiss()` (persists the version), clears the notice when the setting is turned off, logs failures only
- [x] 1.5 Tests: policy cases (newer, same, older, dismissed, newer than dismissed, dev build `x.y.z.a`, Store `x.y.z.0`, disabled), service (check disabled makes no call, failure/timeout shows nothing, dismissal persists across a new service instance, postpone is not persisted, single check per start)

## 2. Windows Store adapter

- [x] 2.1 `StoreUpdateSource` in the App, Windows target only: `StoreContext.GetAppAndOptionalStorePackageUpdatesAsync`, highest version of the app's own package updates, any error or missing version → `Failed`
- [x] 2.2 Register the service only when `PackageIdentity.IsPackaged`; start the check from `App.Start` after monitoring has started
- [x] 2.3 Native update notification reusing the `WindowsNotifier` toast plumbing: version text, no sound, outside the alert chain and tray counter; click shows the main window on the dashboard
- [x] 2.4 Open the Store page with `ms-windows-store://pdp/?PFN=<package family name>`; on failure tell the user to update from the Microsoft Store

## 3. UI

- [x] 3.1 Dashboard banner bound to the current notice: version text, "Open Store", "Not now", "Don't remind me about this version"; `Moth*Brush` resources, readable in Moth and Vesper, footer visible
- [x] 3.2 "Check for new versions" toggle in Settings → General, shown only in the packaged build

## 4. Docs

- [x] 4.1 `docs/privacy.md`: section on the Store version check (what is asked, nothing sent about projects/logs, how to turn it off) and updated date

## 5. Verification

- [x] 5.1 `dotnet build` and `dotnet test` pass on macOS; the zip build shows no setting and makes no check
- [ ] 5.2 Maintainer: on a Store install older than the published version, the banner and one notification appear; "Not now" brings it back after restart; "Don't remind me" silences it after restart; turning the setting off hides it; "Open Store" opens the app's Store page
  - Note: archived with 5.2 open at the maintainer's request (development finished). It can only be checked once a version newer than the installed one (1.12.0 or later) is published in the Store.
