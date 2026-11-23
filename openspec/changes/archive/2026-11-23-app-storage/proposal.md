## Why

Each project has its own Storage section, but there is no place to see the database as a whole. Removed or abandoned projects leave doubts: the user cannot tell what is still stored, which projects are disabled or no longer configured but still hold events, or whether anything is left from projects already removed. Rows left by an interrupted removal are swept at startup, but the freed space is never returned to the disk, and copies of a corrupt database kept next to the file are never cleaned. Besides, the history of active projects only shrinks by hand: the automatic retention (30 days, hidden setting) never removes pending groups, so a project whose events nobody marks as seen grows forever. The user needs one place, in the application settings, to review and remove all of it, and a per-project retention so the database empties itself progressively.

## What Changes

- New **Storage** section in the application settings (reachable from the dashboard) with:
  - **Database summary**: location of the data folder, size of the database file (main file plus write-ahead log), space that a compaction would return to the disk, and the total approximate size of all projects.
  - **Projects list**: every stored project with its state (enabled, disabled, not configured), number of event groups and occurrences, approximate size and last event, sorted by size; per row, actions to open the project, clear its events and remove the project, each after confirmation. Disabled and not-configured projects that still hold data are highlighted as candidates for cleanup.
  - **Leftovers**: data that belongs to no current project or source (sources, read positions, groups, occurrences, hourly counts) and backup copies of a corrupt database left in the data folder, with their counts and sizes; one action removes them after confirmation.
  - **Compact database**: returns the free space to the disk on demand.
- **Per-project retention**: each project gets a retention in days (1–365, default 10), editable in its Storage section. At startup and every hour, history older than the retention is removed automatically, pending groups included, with the same rules as "Keep only the last days"; the freed space is returned to the disk progressively. It replaces the hidden global retention (30 days, seen groups only). **BREAKING**: on the first start of this version, existing projects get 10 days and their older history, pending included, is removed.
- The startup orphan sweep also returns the freed space to the disk when it removed something, in the background.
- Every action here uses the same operations as the project screen (clear events, project removal), so the results are identical; tiles, counters and the project screens refresh afterwards.

## Capabilities

### New Capabilities
- `app-storage`: application-wide view of the local database with per-project usage, removal of projects and their events, removal of leftovers and on-demand compaction, from the application settings.

### Modified Capabilities
- `project-storage`: each project has a retention in days applied automatically.
- `project-alerts`: retention becomes per project and includes pending groups.
- `local-persistence`: the startup orphan sweep returns the freed space to the disk.

## Impact

- Core: `IProjectStorage` gains a usage overview for all projects, leftover counting/removal and corrupt-backup listing; `ProjectService` exposes them; in-memory versions for tests.
- Persistence: `SqliteProjectStorage` (aggregated usage per project in one query, orphan counts, reclaimable space from `freelist_count`/`page_size`), `SqliteDatabase`/`DataDirectory` for the corrupt backups in the data folder.
- App: retention field in `ProjectStorageView` next to the storage threshold; new `AppStorageView` as a section of `AppSettingsView`; reuses the confirmation pattern, theme brushes and the project-removal flow; startup sweep followed by background compaction.
- Migration 8 adds `projects.retention_days` (default 10); `AlertPipeline`'s hourly purge applies it per project instead of the global `alerts.retention_days`, which is no longer read.
- No new dependencies.
