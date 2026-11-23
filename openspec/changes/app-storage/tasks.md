## 1. Retention

- [x] 1.1 Migration 8: `projects.retention_days INTEGER NOT NULL DEFAULT 10`; `Project.RetentionDays` (1–365, default 10) read and written by both repositories
- [x] 1.2 `ProjectService.SetRetentionAsync` with validation, trimming the project at once in the background
- [x] 1.3 Purge tick: per-project `KeepLastDays` instead of `PurgeQuiet`, first tick right after start; remove `AlertOptions.Retention`, `AppSettings.EventRetentionDays` and `IEventStore.PurgeQuiet`
- [x] 1.4 Progressive compaction in `MaintainStorage` (reclaimable ≥ 25% of the file and ≥ 8 MB)
- [x] 1.5 Tests: default 10 for new and migrated projects, old pending group removed, recent group trimmed with totals kept, independent retentions, invalid values rejected, trim at startup, compaction threshold

## 2. Core

- [x] 2.1 Add `ProjectUsageRow`, `LeftoverUsage` and `UsageOfAll`/`Leftovers`/`ReclaimableBytes` to `IProjectStorage`, with the in-memory implementation
- [x] 2.2 Add `IDataFolder` (location, list and delete corrupt backups) and an in-memory fake for tests
- [x] 2.3 Add `AppStorageService`: overview (summary, project rows with state and cleanup candidate, leftovers, backups) and actions (clear project, remove project, remove leftovers, compact) returning result records with removed amounts and freed bytes, all on background tasks
- [x] 2.4 Core tests: overview states and candidates, sorting by size, clear/remove go through `ProjectService`, leftovers removal with a backup that fails to delete, nothing-left-over case

## 3. Persistence

- [x] 3.1 `SqliteProjectStorage.UsageOfAll` as one grouped query, `Leftovers` as orphan counts per table, `ReclaimableBytes` from `freelist_count * page_size`
- [x] 3.2 `SqliteDataFolder` implementing `IDataFolder`, sharing the backup name pattern with `SqliteDatabase.BackupCorruptFile`; never matches the live file or its `-wal`/`-shm`
- [x] 3.3 Persistence tests: usage of several projects matches per-project `Usage`, orphan counts, reclaimable space shrinks after `Compact`, backup listing/deletion ignores other files

## 4. Startup

- [x] 4.1 In `App.Start`, when `RemoveOrphans` removed rows, queue a background compaction after monitoring starts; none when nothing was removed

## 5. UI

- [x] 5.1 Retention field in `ProjectStorageView` next to the storage threshold, with range message

- [x] 5.2 Turn `AppSettingsView` into General / Storage sections (current content under General), footer still visible
- [x] 5.3 `AppStorageView`: summary (data folder, file size with WAL, reclaimable space, total approximate size), refresh, figures computed only when the section is shown or refreshed
- [x] 5.4 Projects list: name, state, groups, occurrences, approximate size, retention, newest event, candidate mark, open / clear events / remove actions with inline confirmations stating the counts; empty-list message
- [x] 5.5 Leftovers block (row counts, backup files with sizes, remove action with confirmation, per-file failures) and Compact action with before/after size
- [x] 5.6 Result line after each action and refresh of the section; theme brushes only (`MothWarningBrush` for candidates), checked in Moth and Vesper

## 6. Wrap-up

- [x] 6.1 `dotnet build` and `dotnet test` green; manual check on macOS with a removed project, a disabled project with events and a fake corrupt backup, and a project with a short retention
- [x] 6.2 Bump the development version (`x.y.z.a`) in `Directory.Build.props`
