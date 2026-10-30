## Why

The team monitors several applications and the question that matters on opening the app is "does any project have something pending?", not "what is scrolling by". The first screen must be a dashboard of projects; each project then has its own screen for its tail and its configuration.

## What Changes

- A `Project` groups log sources under a name, with its own alert configuration (warning threshold, frequency mode).
- **Dashboard** as the home screen: one tile per project showing its status at a glance, plus a "New project" tile. Creating a project asks only for a name and opens its screen.
- **Project screen** opened from a tile, with the live tail scoped to the project and the project's settings (sources, warning threshold, frequency mode, enable/disable, remove). Parameters are edited here, not on the dashboard.
- The global Live and Sources tabs are removed: sources are managed inside each project.
- Existing sources from earlier versions are kept by moving them into a project.

## Capabilities

### New Capabilities
- `projects`: definition, persistence and configuration of monitored projects.
- `project-dashboard`: home screen with a status tile per project.
- `project-screen`: per-project screen with the tail and the settings.

### Modified Capabilities
- `log-sources`: sources belong to a project; existing sources are migrated.

## Impact

- New code in `MothEidol.Core` (project model, repository, status) and `MothEidol.App` (dashboard, project screen); the viewer is reused inside the project screen.
- Uses `local-persistence` (new migration) and `core-log-tailing` / `serilog-parsing`.
- Provides the configuration consumed by `project-alerts`, which adds the events section and the "pending" state to this screen and to the tiles.
- Replaces the Live and Sources tabs introduced in `core-log-tailing`.
