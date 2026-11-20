## Context

See proposal.md for motivation. Current state relevant to the approach:

- `SqliteDatabase.CreateConnection` opens a new connection with `Pooling = false` for every repository call. Hot callers: `SqliteEventStore.Record` (every alert-level entry), `SqliteReadPositionStore.Save` (every poll with new data, per source), and the UI timers (dashboard every 1.5 s calls `Projects`, `StatusOf` → `Get` → `List`, `Pending` and `ProjectSeries` per project; the events section every 1.5 s). With WAL, closing the last open connection checkpoints and may delete the `-wal` file, so the pattern repeats that cost on every call.
- WAL is enabled but `synchronous` stays at the default `FULL`; there is no busy timeout, so concurrent writers from the tailing, alert and UI threads can get `SQLITE_BUSY`.
- Growth limits that already exist: at most 50 occurrences per group (`AlertOptions.MaxOccurrencesPerGroup`), hourly counts pruned after 8 days, seen groups quiet for longer than the retention (30 days default) purged hourly. Unbounded: pending groups (never purged automatically), the number of groups, and the file itself (`auto_vacuum` is off, so deleted pages go to the freelist and the file never shrinks).
- `ProjectService.RemoveAsync` removes each source (stops its monitor, deletes the source row and its read position), then the project row, then the events: four separate transactions; a failure midway leaves orphans.
- Tiles and the events section refresh on timers, so they pick up a cleanup without a new notification path.

## Goals / Non-Goals

**Goals:**
- Per-project usage figures and manual cleanup, behind a Core abstraction testable without the UI.
- Project removal in one transaction and a startup orphan sweep.
- Cheaper storage hot path with no observable behavior change.

**Non-Goals:**
- Automatic per-project size limits or scheduled cleanups (the existing retention keeps working as is).
- Changing the per-group occurrence limit or the retention setting UI.
- Caching the dashboard queries (pooling removes most of their cost; revisit only if measured).

## Decisions

### Core abstraction: `IProjectStorage`
A new Core interface, separate from `IEventStore` so the alert pipeline does not grow cleanup concerns:

```
ProjectStorageUsage Usage(long projectId);              // groups, occurrences, buckets, approx bytes, oldest/newest occurrence
DatabaseFileUsage File();                               // main file + WAL size
CleanupResult ClearAll(long projectId);                 // removed groups, occurrences
CleanupResult KeepLastDays(long projectId, DateTimeOffset cutoff);
void Compact();                                         // return free pages to the disk
void Checkpoint();                                      // WAL truncate + PRAGMA optimize
int RemoveOrphans();
```

Implemented by `SqliteProjectStorage` in Persistence and an in-memory version over `InMemoryEventStore` for Core tests. `ProjectService` gains `ClearEventsAsync`/`KeepLastDaysAsync`, which run the cleanup and then `Compact` on a background task (`Task.Run`), so the UI only awaits.

### Approximate size from column lengths
`SUM(length(message) + COALESCE(length(context), 0))` over the project's occurrences plus `length(title|key|location)` over its groups, plus a fixed per-row overhead (≈ 40 bytes per occurrence/group, 16 per bucket). It is labeled "approx." in the UI. Alternative: the `dbstat` virtual table gives exact page usage but is per table, not per project, and is not guaranteed in the bundled SQLite build.

### Keep the last N days
In one transaction, with `cutoff` in UTC ms and `cutoffHour = cutoff_seconds / 3600`:
1. Remove occurrences and buckets of groups of the project with `last_seen < cutoff`, then those groups (pending included).
2. Remove remaining occurrences of the project's groups with `at < cutoff`.
3. Remove the project's buckets with `hour < cutoffHour`.

Group `count`, `first_seen` and `pending_count` of kept groups are not recomputed: the totals keep meaning "seen in total", matching how the per-group occurrence limit already works. The existing `ix_event_occurrences_group` index and the per-project groups index are enough (occurrences are reached through their groups), so no migration is added. Days are validated in the view (1–365, default 7) and converted to a cutoff with `TimeProvider`.

### Clear all
Same transaction as `IEventStore.RemoveProject` (occurrences, buckets, groups of the project); exposed through `IProjectStorage` with counts. Read positions are not touched, so the tail resumes where it was and old lines are not re-alerted. A frequency policy may still hold a summary for cleared events; it is delivered normally (acceptable, it describes what happened before the cleanup).

### Atomic project removal
`TailingService` gains `UnloadProjectAsync(projectId)`: stops the project's monitors and forgets their statuses without touching storage. `IProjectRepository.Remove` becomes the single transaction deleting the project's read positions, sources, occurrences, buckets, groups and the project row (the SQLite repository does it in SQL; the in-memory one in step). `ProjectService.RemoveAsync` = unload monitors → repository remove → refresh the source list in `TailingService` → `Compact` in the background. `IEventStore.RemoveProject` stays for the in-memory path but is no longer called on removal. If the transaction fails, monitors are reloaded so the project keeps working.

Alternative considered: foreign keys with `ON DELETE CASCADE`. Rejected: the existing tables have no FK constraints and adding them requires rebuilding the tables in a migration; the explicit transaction gives the same result.

### Orphan sweep at startup
`RemoveOrphans` runs once after migrations (in `App` startup, before monitors start): deletes `log_sources` whose `project_id` has no project, `read_positions` without a source, groups without a project, occurrences and buckets without a group. Cheap (`NOT IN` on primary keys) and idempotent.

### Compaction with `VACUUM`
After a manual cleanup or a project removal, run `VACUUM` and then `PRAGMA wal_checkpoint(TRUNCATE)`. `VACUUM` rewrites the file, needs no other transaction open at that moment and takes seconds on a large database; it runs on a background task and a `SQLITE_BUSY` is retried once after a short delay, otherwise skipped (the next cleanup compacts). Alternative: `auto_vacuum = INCREMENTAL` with `PRAGMA incremental_vacuum`. Rejected for now: switching an existing database to incremental needs a full `VACUUM` anyway and it fragments the file; manual cleanups are rare, so a full `VACUUM` on demand is simpler.

### Connection and pragma tuning
- Pooling on (the Microsoft.Data.Sqlite default). `SqliteConnection.ClearAllPools()` is called before the corrupt-file backup moves the file, and on shutdown so the WAL is checkpointed on close.
- `Default Timeout = 5` in the connection string, which makes Microsoft.Data.Sqlite retry on `SQLITE_BUSY` for up to 5 s.
- `PRAGMA synchronous = NORMAL` on every opened connection (cheap; it is per connection). With WAL this keeps the database consistent on power loss and may lose only the last transactions, acceptable for event history that comes from log files.
- `Checkpoint` (`wal_checkpoint(TRUNCATE)` + `PRAGMA optimize`) runs in the alert pipeline's existing hourly purge tick, right after `PurgeQuiet`/`PruneBuckets`, so the `-wal` file cannot grow while pooled connections stay open.

## Risks / Trade-offs

- [`VACUUM` needs free disk space up to the database size and blocks writers while it runs] → only on manual actions and project removal, in the background; writers wait up to the busy timeout and the tail keeps reading in memory meanwhile; if it fails it is skipped and reported nowhere but a log line.
- [`synchronous = NORMAL` can lose the last committed events on power loss] → events come from log files that are still on disk; read positions of the same transaction window are lost too, so the lines are re-read and re-recorded.
- [Pooled connections keep the file open, which matters for the corrupt-file backup and for uninstall] → pools cleared before the backup and on exit.
- [Cleared pending groups disappear without being seen] → explicit confirmation stating that pending events are included and how many.
- [Approximate size differs from the real file share] → labeled approx.; the whole file size is shown next to it.

## Migration Plan

No schema migration. The orphan sweep cleans data left by earlier versions on the first start. Rollback: an older build opens the same schema version unchanged.
