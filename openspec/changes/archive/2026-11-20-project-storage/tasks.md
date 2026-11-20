## 1. Storage tuning (no behavior change)

- [x] 1.1 `SqliteDatabase`: enable pooling, `Default Timeout = 5`, `PRAGMA synchronous = NORMAL` on every opened connection; clear pools before the corrupt-file backup and on shutdown
- [x] 1.2 Persistence tests still pass; add a test that concurrent writers on two threads do not fail with `SQLITE_BUSY`

## 2. Core: project storage

- [x] 2.1 Add `IProjectStorage` with `ProjectStorageUsage`, `DatabaseFileUsage` and `CleanupResult` records, and an in-memory implementation over the in-memory event store
- [x] 2.2 `ProjectService`: `UsageOf`, `ClearEventsAsync`, `KeepLastDaysAsync` (cutoff from `TimeProvider`, days 1–365) running cleanup and compaction off the UI thread
- [x] 2.3 Core tests: clear all keeps sources/read positions and other projects; keep-last-days removes old groups (pending included), trims old occurrences and buckets of kept groups and keeps their totals and pending state; invalid days rejected

## 3. Persistence: project storage

- [x] 3.1 `SqliteProjectStorage.Usage` and `File` (counts, approximate bytes, oldest/newest occurrence, main file + WAL size)
- [x] 3.2 `ClearAll` and `KeepLastDays` in one transaction each, returning removed groups and occurrences
- [x] 3.3 `Compact` (`VACUUM` + `wal_checkpoint(TRUNCATE)`, one retry on busy) and `Checkpoint` (`wal_checkpoint(TRUNCATE)` + `PRAGMA optimize`)
- [x] 3.4 `RemoveOrphans` (sources without project, read positions without source, groups without project, occurrences and buckets without group)
- [x] 3.5 Persistence tests for 3.1–3.4, including a file that shrinks after cleanup + compaction

## 4. Atomic project removal

- [x] 4.1 `TailingService.UnloadProjectAsync` (stop monitors, forget statuses, no storage changes) and reload of its source list afterwards
- [x] 4.2 `IProjectRepository.Remove` deletes the project with its sources, read positions, groups, occurrences and buckets in one transaction (SQLite and in-memory)
- [x] 4.3 `ProjectService.RemoveAsync`: unload → repository remove → compact in background; reload monitors if the removal fails
- [x] 4.4 Tests: nothing of the project left in any table; other projects untouched; recreating a project with the same name starts empty

## 5. Storage warning

- [x] 5.1 Migration 7: `projects.storage_warning_mb INTEGER NOT NULL DEFAULT 100`; `Project.StorageWarningMb` (1–10240, default 100) read/written by both repositories
- [x] 5.2 `ProjectService`: in-memory size cache per project, `StorageWarningOf(projectId)`, `RefreshStorageSizesAsync()` (all projects, background), refresh of one project after a cleanup, a measure from the storage section or a threshold change
- [x] 5.3 Core tests: default threshold, over/under after measuring, warning cleared after a cleanup, threshold change re-evaluated without a new measure, not over before the first measure; persistence test for the threshold round-trip and the migration default

## 6. Wiring

- [x] 6.1 App startup: run `RemoveOrphans` after opening the database, before monitors start; then measure storage sizes in the background
- [x] 6.2 Alert pipeline hourly purge tick also calls `Checkpoint` and refreshes the storage sizes

## 7. UI: Storage section

- [x] 7.1 `ProjectStorageView` (Storage tab next to Settings in `ProjectView`): usage figures with relative/absolute dates, database file size, refresh button; theme brushes only
- [x] 7.2 "Clear all" and "Keep the last [N] days" actions with confirmation stating how many groups (and pending ones) will be removed; disabled when the project has no events or the days are invalid
- [x] 7.3 Busy state while running, result message (removed groups/occurrences, new file size), figures refreshed; version footer stays visible
- [x] 7.4 Threshold field in the Storage section (whole MB, 1–10240, range shown when invalid) and highlighted over-threshold message above the cleanup actions (`MothWarningBrush`)
- [x] 7.5 Dashboard tile: "Storage over N MB" line from `StorageWarningOf`, status dot unchanged

## 8. Validation and release

- [x] 8.1 `dotnet build` and `dotnet test`
- [ ] 8.2 Launch the app, fill a project with the log simulator, check the figures, both cleanups, the file size dropping, the storage warning (lower the threshold until the tile shows it, clean until it disappears) and removing the project (manual check by the maintainer)
- [x] 8.3 Bump the development version `1.10.0.a` in `Directory.Build.props` (1.10.0.1)
