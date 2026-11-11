## Why

Each team member runs the app on their own PC and configures every project by hand. There is no way to share a project's setup with a colleague, back it up, or move it to a new machine.

## What Changes

- A project's configuration can be exported to a single JSON file and imported on another machine or into the same app.
- The file contains name, accent, source definitions (folders and patterns), alert levels, frequency policy and, when they exist, keywords and rules. It never contains events, offsets, read state or application settings.
- Import is previewed and validated before anything is created; it creates a new project (never overwrites silently) and reports sources that do not exist on this machine so the user can fix paths.
- Export/import is reachable from the project settings (export) and the dashboard (import).

## Capabilities

### New Capabilities
- `project-config-transfer`: export and import of project configuration.

### Modified Capabilities
- `project-dashboard`: import entry point next to the "New project" tile.

## Impact

- Core: versioned configuration document (DTO), serialization, validation, import mapping onto the existing atomic project creation.
- App: export action in project settings, import action and preview dialog on the dashboard, file pickers.
- No new dependencies (System.Text.Json).
