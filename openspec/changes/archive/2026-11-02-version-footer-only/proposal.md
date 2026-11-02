## Why

The version in the window title and the tray tooltip is visual noise and shows up in screenshots and store material. The persistent footer is enough to identify the running build.

## What Changes

- The main window title and the tray tooltip show only the application name, without the version.
- The persistent footer of the main window remains the single place where the version is shown.
- Release artifacts and image assets carry no version text; the version stays only in file names and package metadata that the platforms require.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `app-shell`: the version is visible only in the persistent footer, no longer in the window title or the tray tooltip.

## Impact

- `MainWindow` title and `TrayPresenter` tooltip; `AGENTS.md` versioning rule.
- No data, dependency or version bump impact.
