## 1. Storage tuning (no behavior change)

- [ ] 1.1 `SqliteDatabase`: enable pooling, `Default Timeout = 5`, `PRAGMA synchronous = NORMAL` on every opened connection; clear pools before the corrupt-file backup and on shutdown
- [ ] 1.2 Persistence tests still pass; add a test that concurrent writers on two threads do not fail with `SQLITE_BUSY`

## 2. Core: project storage

- [ ] 2.1 Add `IProjectStorage` with `ProjectStorageUsage`, `DatabaseFileUsage` and `CleanupResult` records, and an in-memory implementation over the in-memory event store
- [ ] 2.2 `ProjectService`: `UsageOf`, `ClearEventsAsync`, `KeepLastDaysAsync` (cutoff from `TimeProvider`, days 1–365) running cleanup and compaction off the UI thread
- [ ] 2.3 Core tests: clear all keeps sources/read positions and other projects; keep-last-days removes old groups (pending included), trims old occurrences and buckets of kept groups and keeps their totals and pending state; invalid days rejected

## 3. Persistence: project storage

- [ ] 3.1 `SqliteProjectStorage.Usage` and `File` (counts, approximate bytes, oldest/newest occurrence, main file + WAL size)
- [ ] 3.2 `ClearAll` and `KeepLastDays` in one transaction each, returning removed groups and occurrences
- [ ] 3.3 `Compact` (`VACUUM` + `wal_checkpoint(TRUNCATE)`, one retry on busy) and `Checkpoint` (`wal_checkpoint(TRUNCATE)` + `PRAGMA optimize`)
- [ ] 3.4 `RemoveOrphans` (sources without project, read positions without source, groups without project, occurrences and buckets without group)
- [ ] 3.5 Persistence tests for 3.1–3.4, including a file that shrinks after cleanup + compaction

## 4. Atomic project removal

- [ ] 4.1 `TailingService.UnloadProjectAsync` (stop monitors, forget statuses, no storage changes) and reload of its source list afterwards
- [ ] 4.2 `IProjectRepository.Remove` deletes the project with its sources, read positions, groups, occurrences and buckets in one transaction (SQLite and in-memory)
- [ ] 4.3 `ProjectService.RemoveAsync`: unload → repository remove → compact in background; reload monitors if the removal fails
- [ ] 4.4 Tests: nothing of the project left in any table; other projects untouched; recreating a project with the same name starts empty

## 5. Wiring

- [ ] 5.1 App startup: run `RemoveOrphans` after opening the database, before monitors start
- [ ] 5.2 Alert pipeline hourly purge tick also calls `Checkpoint`

## 6. UI: Storage section

- [ ] 6.1 `ProjectStorageView` (Storage tab next to Settings in `ProjectView`): usage figures with relative/absolute dates, database file size, refresh button; theme brushes only
- [ ] 6.2 "Clear all" and "Keep the last [N] days" actions with confirmation stating how many groups (and pending ones) will be removed; disabled when the project has no events or the days are invalid
- [ ] 6.3 Busy state while running, result message (removed groups/occurrences, new file size), figures refreshed; version footer stays visible

## 7. Validation and release

- [ ] 7.1 `dotnet build` and `dotnet test`
- [ ] 7.2 Launch the app, fill a project with the log simulator, check the figures, both cleanups, the file size dropping, and removing the project (manual check by the maintainer)
- [ ] 7.3 Bump the development version `1.10.0.a` in `Directory.Build.props`
