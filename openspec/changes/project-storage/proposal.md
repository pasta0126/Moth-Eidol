## Why

The local database only grows: groups that stay pending are never purged, every retained occurrence keeps its full text and context, and SQLite never returns deleted space to the disk, so the file never shrinks. Users cannot see how much a project weighs or lighten it, and removing a project is not guaranteed to leave the database clean if it fails midway. The history must stay manageable so it never harms the app or the PC.

A review of the storage layer also found avoidable cost on the hot path: a new unpooled connection is opened for every read and write (each close of the last connection checkpoints the WAL), writes use the WAL default `synchronous=FULL` (one fsync per recorded occurrence), and there is no busy timeout between the tailing, alert and UI threads.

## What Changes

- New **Storage** section in the project screen, next to Settings, showing the project's load in the database: event groups, retained occurrences, hourly counts, approximate size, oldest and newest event, and the whole database file size.
- **Clear all**: removes every event of the project (groups, occurrences, hourly counts), pending ones included, after confirmation. Sources, settings and read positions are kept, so already-read lines are not re-alerted.
- **Keep the last N days**: removes the project's occurrences and hourly counts older than N days and the groups with no activity in that period, pending ones included, after confirmation; group totals stay as they are.
- After a manual cleanup the freed space is returned to the disk (compaction), so the file actually shrinks.
- Removing a project deletes everything related in one transaction (project, sources, read positions, groups, occurrences, hourly counts); at startup, rows left by a project that no longer exists are removed.
- Storage tuning with no behavior change: pooled connections, `synchronous=NORMAL` with WAL, a busy timeout, and a periodic WAL checkpoint/`PRAGMA optimize`.

## Capabilities

### New Capabilities
- `project-storage`: per-project database usage view and manual cleanup (clear all, keep the last N days) with disk compaction.

### Modified Capabilities
- `project-screen`: the project screen gains the Storage section.
- `projects`: removing a project removes all its stored data atomically and leaves no orphans.
- `local-persistence`: orphan cleanup at startup and space reclamation.

## Impact

- Core: `IEventStore` gains usage and cleanup operations (with the in-memory store), `ProjectService` exposes them and removes projects atomically through a new repository operation.
- Persistence: `SqliteEventStore`, `SqliteProjectRepository`, `SqliteDatabase` (connection string, pragmas, orphan sweep, compaction). No schema migration needed; an index on `event_occurrences (at)` may be added if the day cutoff needs it (migration 7).
- App: new `ProjectStorageView` and a fourth section in `ProjectView`; uses existing theme brushes and the confirmation pattern of project removal.
- No new dependencies.
