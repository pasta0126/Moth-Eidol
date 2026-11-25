## Context

See proposal.md for the motivation and specs/update-check for the behavior. Relevant current state:

- `MothEidol.App` already targets `net10.0-windows10.0.19041.0` for `win-*` runtime identifiers, so the WinRT `Windows.Services.Store` API is reachable from the Windows build without new packages; other builds target plain `net10.0`.
- `PackageIdentity.IsPackaged` (Core) tells whether the process runs with an MSIX identity. The MSIX is only distributed through the Store (a locally signed test package also has an identity; see Risks).
- `WindowsNotifier` already shows native toasts (AUMID, click brings the window to the front) for alerts through the notifier chain.
- `AppSettings` wraps the SQLite key/value settings table with typed properties; new keys need no migration.
- The MSIX version is the `Directory.Build.props` version plus `.0` (`x.y.z.0`); the running version comes from `AppVersion`.

## Goals / Non-Goals

**Goals:**
- Keep the decision "announce or not, which version" in Core, pure and unit-tested; the Store call is a thin Windows-only adapter.
- Zero cost for zip builds: nothing registered, nothing contacted.

**Non-Goals:**
- Installing updates (`RequestDownloadAndInstallStorePackageUpdatesAsync`), mandatory updates, or a "check now" button.
- Checking GitHub Releases for zip installs.

## Decisions

### D1. `IUpdateSource` in Core, Store adapter in App
`IUpdateSource.GetLatestAsync(CancellationToken) → UpdateCheckResult` (`NoUpdate`, `Available(Version)`, `Failed(reason)`). `StoreUpdateSource` (App, compiled only for the Windows target via `#if WINDOWS` / the TFM condition) calls `StoreContext.GetDefault().GetAppAndOptionalStorePackageUpdatesAsync()` and takes the highest `update.Package.Id.Version` among the app's own package updates. Any exception or an update without a usable version → `Failed`.
*Alternative:* query the public Store catalog over HTTP from every platform. Rejected: unofficial endpoint, and zip installs cannot update from the Store anyway.

### D2. `UpdateNoticePolicy` decides
Pure function in Core: `(checkEnabled, runningVersion, dismissedVersion, result) → notice or none`. Announce only when the result is `Available(v)`, `v > running` and `v != dismissed`. Versions are compared on `major.minor.patch` only: the Store's fourth part is always `.0`, and a dev build `x.y.z.a` counts as `x.y.z` (it is never the Store build, but keeps the comparison total). The dismissed version is stored as `x.y.z`.
*Why "≠ dismissed" and not "> dismissed":* the spec only requires silence for that exact version; if the Store ever rolls back below a dismissed version, it is still newer than the running one and worth telling. In practice both behave the same.

### D3. `UpdateCheckService` orchestrates once per start
Core service, started from `App.Start` after monitoring has started, on a background task with a 30 s timeout. It reads the setting, calls the source, applies the policy and raises `NoticeAvailable(version)` once. It keeps the current notice in memory so the dashboard can show the banner when it is (re)created, and exposes `Postpone()` (in-memory only, "Not now") and `Dismiss()` (persists `update.dismissed_version`). Failures are logged through the existing application log, never surfaced.

### D4. Settings
`AppSettings.CheckForUpdates` (`update.check_enabled`, default `true`) and `AppSettings.DismissedUpdateVersion` (`update.dismissed_version`, default empty). The toggle lives in Settings → General and is shown only when `PackageIdentity.IsPackaged`. Turning it off clears the in-memory notice (banner disappears); turning it on does not trigger a check (next start).

### D5. Notification outside the alert chain
The update notice is not an alert: it does not go through `NotificationDispatcher` (no sound, no tray counter, no channel settings). The App reuses the native toast plumbing of `WindowsNotifier` (same AUMID) with a dedicated method/text "Moth-Eidol x.y.z is available in the Microsoft Store"; clicking it shows the main window and navigates to the dashboard. If the native toast fails, nothing else is shown (the banner is enough). Since the Store build is always Windows, no in-app toast fallback is needed.

### D6. Opening the Store page
`ms-windows-store://pdp/?PFN=<PackageFamilyName>` with the family name taken from the running package identity, so no product id is hard-coded or kept in the repo. If launching fails, the banner shows a short message "Update from the Microsoft Store".

### D7. Banner placement
A `DashboardView` strip above the tiles, styled with `Moth*Brush` resources (info/accent, readable in both themes), with the version text and three buttons. It is bound to the service's current notice, so it survives navigation and theme changes, and it never covers the footer.

## Risks / Trade-offs

- [`StoreContext` in a Win32 full-trust desktop app may need `IInitializeWithWindow`] → only purchase/install UI needs a window handle; the query does not. Verify on the real Store install (task); if it fails, initialize it with the main window handle.
- [Locally signed test MSIX has an identity but no Store listing] → the query returns an error or no update → silent per spec.
- [Store reports an update before it is available in the user's market / during staged rollout] → acceptable: the user simply sees "up to date" in the Store and can dismiss.
- [Package update without a readable version] → treated as `Failed`, so nothing is announced rather than an unversioned banner that could not be dismissed per version.
- [The check cannot be verified on macOS, the dev machine] → policy and service are covered by unit tests with fake sources; the adapter is verified manually by the maintainer on a Store install of a version older than the published one.

## Migration Plan

No schema change. Existing Store users get the setting on (default) at the first start of the version containing this change; the first possible announcement is for the release after it.
