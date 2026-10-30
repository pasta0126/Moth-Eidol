## Why

The team monitors several applications, each writing its own logs in its own folder and with its own noise level. Alerting must be decided per project, not per file: which folder belongs to which project, from which level it alerts and how often.

## What Changes

- A `Project` groups one or more log sources (folder plus pattern, or file) under a name.
- Per-project alert configuration: warning threshold (alert on warnings or not; error and above always alert) and frequency mode (immediate with cooldown, or periodic summary with an interval).
- Projects screen to add, edit, enable/disable and remove projects.
- Project-level status summary (sources up, last entry time, recent errors/warnings count).
- The viewer lets the user pick a project and its sources.

## Capabilities

### New Capabilities
- `projects`: definition, persistence and status of monitored projects and their alert configuration.

### Modified Capabilities
- `log-sources`: sources belong to a project.

## Impact

- New code in `MothEidol.Core` (project model, repository) and `MothEidol.App` (projects screen).
- Uses `local-persistence` for storage and `core-log-tailing` / `serilog-parsing` for entries.
- Provides the configuration consumed by `project-alerts`.
