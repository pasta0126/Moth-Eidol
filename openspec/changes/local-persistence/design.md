## Context

Single-user desktop application with no server. See `proposal.md` for motivation. Depends on `app-shell` (solution structure).

## Goals / Non-Goals

**Goals:**
- Simple, embedded, zero-configuration persistence.
- Access from the core through abstractions, not coupled to SQLite.

**Non-Goals:**
- Synchronization or multi-user.
- Database encryption.

## Decisions

- **SQLite** via `Microsoft.Data.Sqlite`. Alternatives: LiteDB (less standard), JSON files (no queries or transactions for history).
- **Access**: direct SQL with per-concept repositories, defined as interfaces in `Core` and implemented in a `MothEidol.Persistence` project. An ORM is overkill for the expected volume.
- **Migrations**: numbered SQL scripts; the version is stored in `PRAGMA user_version`.
- **Location**: the OS application data directory (`ApplicationData` / `XDG_DATA_HOME` / `~/Library/Application Support`).
- **Concurrency**: WAL mode and a single serialized write connection.

## Risks / Trade-offs

- [The data directory may be on a network profile] → document that it must be local; the database is never placed in shared folders.
- [History growth] → retention is handled in the `alert-rules` change.
