## Context

See proposal.md for motivation. Current state relevant to the approach:

- `IProjectStorage` (Core) with `SqliteProjectStorage` (Persistence) already offers per-project `Usage`, `File`, `ClearAll`, `KeepLastDays`, `Compact`, `Checkpoint` and `RemoveOrphans`. `ProjectService` wraps them (`ClearEventsAsync`, `RemoveAsync`, background `Compact`, cached sizes for the tile warning).
- `RemoveOrphans` runs once at startup in `App.Start`, before monitoring, and its result is ignored: no compaction follows, so swept space stays in the freelist.
- A corrupt database is moved aside as `moth-eidol.db.corrupt-<yyyyMMddHHmmss>` in the data folder and never removed.
- `AppSettingsView` is a single stacked panel (autostart, channels, sound, theme, about); `MainWindow` already knows how to open a project screen (`OpenProject`).
- The alert pipeline's purge tick (`PurgeInterval`, hourly) calls `IEventStore.PurgeQuiet(now - Retention)` with the hidden global `alerts.retention_days` (30, no UI; seen groups only) and `PruneBuckets(now - 8 days)`, then `ProjectService.MaintainStorage()`. `IProjectStorage.KeepLastDays(projectId, cutoff)` already implements the trimming rule the per-project retention needs.
- `Usage(projectId)` runs one statement with sub-selects per project; calling it N times for the overview is fine for a handful of projects but repeats scans.

## Goals / Non-Goals

**Goals:**
- One Core entry point for the application-wide overview and cleanup, testable without the UI.
- Reuse the existing clear and remove flows so the application section and the project screen can never diverge.

**Non-Goals:**
- Automatic cleanups or scheduled removal of disabled projects.
- Removing files outside the data folder (log files, the app itself) or the `-wal`/`-shm` files of the live database.
- Exact per-project page usage (`dbstat`); the approximate size stays as defined in `project-storage`.
- Keep-last-N-days from the application section (it stays per project, where the days are chosen).
- A per-level or per-state retention (seen vs pending); one number per project.

## Decisions

### Settings screen with sections
`AppSettingsView` becomes a `TabControl` with **General** (the current content, unchanged) and **Storage** (new `AppStorageView`), mirroring the project screen's sections. The footer stays visible. Alternative: append the storage block to the existing stacked panel; rejected because the project list makes the panel too long and the figures would be computed whenever settings open. The Storage tab computes its figures only when it is selected or refreshed.

### Core additions to `IProjectStorage`
```
IReadOnlyList<ProjectUsageRow> UsageOfAll();   // project id → groups, pending groups, occurrences, approx bytes, newest occurrence
LeftoverUsage Leftovers();                     // orphan rows per table (no deletion)
long ReclaimableBytes();                       // PRAGMA freelist_count * PRAGMA page_size
```
`RemoveOrphans` already exists and is reused for the removal. `UsageOfAll` is one grouped query (`GROUP BY project_id` over groups, occurrences joined through groups) instead of N `Usage` calls; projects without events are completed from the project list with zeros. The in-memory implementation derives the same from `InMemoryEventStore`.

Corrupt backups are file-system, not SQL: a new `IDataFolder` abstraction in Core (`Location`, `ListCorruptBackups()`, `Delete(backup)`) implemented in Persistence next to `DataDirectory`, matching only `moth-eidol.db.corrupt-*` in the data folder so nothing else can be deleted. `BackupCorruptFile` and the listing share the naming constant.

### `AppStorageService` (Core)
A small Core service composed of `ProjectService`, `IProjectStorage` and `IDataFolder` that builds the overview (`Overview()` → summary, rows with state from `ProjectService` — enabled / disabled / not configured when the project has no sources —, leftovers, backups) and runs the actions:
- `ClearProjectAsync(id)` → `ProjectService.ClearEventsAsync` (same path as the project screen, so sizes cache, compaction and counters behave the same).
- `RemoveProjectAsync(id)` → `ProjectService.RemoveAsync`.
- `RemoveLeftoversAsync()` → `RemoveOrphans`, delete each backup (failures collected per file, not thrown), then `Compact`.
- `CompactAsync()` → `Compact` with file size before/after.
Each returns a result record with removed amounts and freed bytes (file size before minus after). All run on a background task; the view only awaits. Cleanup candidate = state is disabled or not configured, and groups > 0.

### Refreshing other views
Tiles, the events section and the tray counter already refresh on timers or on the pending-count notification from the event store; clearing and removal go through `ProjectService`, which raises the same notifications as from the project screen. No new notification path is added.

### Opening a project from the list
`AppStorageView` receives an `Action<Project>` from `MainWindow` (its existing `OpenProject(project, settingsFirst: false)`), like the dashboard does.

### Startup sweep followed by compaction
`App.Start` keeps the sweep before monitoring; when it returns more than zero rows, a background `Compact` is queued after the monitors start (the existing `CompactQuietly` path, which retries once on `SQLITE_BUSY` and otherwise gives up). Nothing removed → nothing run.

### Confirmations
Same inline confirmation pattern as project removal (a panel with "Yes, …" / "Cancel"), with the counts in the text (pending groups for clear; sources and groups for remove; rows and files for leftovers). Compact needs no confirmation: it removes nothing.

### Per-project retention
`Project` gains `RetentionDays` (1–365, default 10), stored by migration 8 as `projects.retention_days INTEGER NOT NULL DEFAULT 10`, edited in the project's Storage section next to the storage threshold and saved through `ProjectService` (`SetRetentionAsync`, validated like the threshold); the wizard does not ask for it.

The purge tick replaces `PurgeQuiet(now - Retention)` with a per-project `KeepLastDays(project, now - RetentionDays)` for every project, then `PruneBuckets(now - 8 days)` as today (buckets older than a shorter retention are already removed by `KeepLastDays`). The first tick runs right after the pipeline starts (today `lastPurge` starts at the start time; it starts at `MinValue` instead), so history accumulated while the app was closed is trimmed shortly after startup. Changing a project's retention runs `KeepLastDays` for it at once on a background task. `AlertOptions.Retention`, `AppSettings.EventRetentionDays` and `IEventStore.PurgeQuiet` are removed (the stored setting row is ignored and harmless). Alternative: keep the global retention as a fallback; rejected, two overlapping rules for the same history would be confusing and the global one has no UI.

Removed pending groups lower the pending counts through the same path as a manual cleanup; the frequency policies are not told, so a summary already held is delivered normally (as with "Clear all").

### Progressive compaction
A full `VACUUM` every hour would rewrite the file for small gains. After the purge tick, `ProjectService.MaintainStorage` compacts only when `ReclaimableBytes()` is at least 25% of the file and at least 8 MB; otherwise the free pages are reused by new events, which already keeps the file from growing. This is what "returned to the disk progressively" means in the spec.

## Risks / Trade-offs

- [First start after upgrading removes everything older than 10 days, pending included] → stated in the changelog entry; the release note recommends reviewing pending events before updating.
- [A pending error that stopped happening more than N days ago disappears unseen] → chosen by the maintainer; the retention is visible and editable per project and shown in the application Storage list.

- [`UsageOfAll` scans all occurrences] → only on opening or refreshing the tab, on a background task; same cost as one project's usage today, done once.
- [Deleting the wrong file] → only names matching the backup pattern inside the data folder are listed and deleted; the live file and its sidecars never match.
- [A project removed from here while its screen is open elsewhere] → only one main window exists and the settings screen replaces the project screen; when the user returns, the project is gone and the dashboard is shown.
- [Compaction blocks writers for seconds on a large file] → background task, busy timeout, tail keeps reading in memory (as in `project-storage`).

## Migration Plan

Migration 8 adds `projects.retention_days` with default 10. On the first start of this version the sweep compacts if it finds leftovers from earlier versions. Rollback: an older build refuses the newer schema, as with any migration.
