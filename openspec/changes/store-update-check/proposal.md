## Why

Users who installed Moth-Eidol from the Microsoft Store have no way to know, from inside the app, that a newer version has been published; they only find out if they happen to open the Store. The app should tell them, and leave the decision to update to them.

## What Changes

- At startup, the Store-installed (MSIX) build asks the Microsoft Store whether a newer version of the app is published. One check per start; no periodic checks while it keeps running.
- New general application setting "Check for new versions", on by default, shown only in the Store-installed build. Zip installs (Windows, macOS, Linux) neither show the setting nor check.
- When a newer version exists and the user has not dismissed that exact version:
  - a banner on the dashboard names the version and offers **Open Store** (opens the app's Store page), **Not now** (hides the banner until the next start) and **Don't remind me about this version**;
  - one Windows notification per start announces it.
- Until the user dismisses a version, the reminder comes back on every start. A dismissed version is never announced again, but a later, newer version is.
- The app never downloads or installs the update itself; updating happens in the Store.
- A check that cannot complete (no network, Store unavailable, any error) is silent and is retried only at the next start.
- The "no data collection" rule and the privacy policy are updated to state the Store query: it sends nothing about the user, projects or logs.

## Capabilities

### New Capabilities
- `update-check`: the startup query to the Microsoft Store, the "Check for new versions" setting, the dashboard banner and notification, and per-version dismissal.

### Modified Capabilities
- `support-links`: "No data collection" now allows the Store version query, and the privacy policy describes it.

## Impact

- Core: a platform-independent update-notice decision (compare versions, apply dismissal and the setting) behind an `IUpdateSource` abstraction, unit-tested.
- App (Windows build only): a Store-backed `IUpdateSource` using `Windows.Services.Store.StoreContext`, already available through the existing `net10.0-windows10.0.19041.0` target; no new NuGet dependency.
- Settings: two new keys in the existing SQLite settings table (`update.check_enabled`, `update.dismissed_version`); no migration.
- UI: dashboard banner, setting in Settings → General.
- Docs: `docs/privacy.md` (new section, updated date).
- Only the Store build is affected; zip builds behave as today.
