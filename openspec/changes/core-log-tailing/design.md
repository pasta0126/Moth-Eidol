## Context

The core must be independent from the UI and the platform, and work with local files and SMB paths. See `proposal.md`. Depends on `local-persistence` for sources and positions.

## Goals / Non-Goals

**Goals:**
- Reliable tail across rotation, truncation and network drops.
- A core testable with temporary files, without a UI.

**Non-Goals:**
- Multi-line grouping (stack traces as a single event).
- Format-specific parsers.
- Folders with glob patterns.

## Decisions

- **Polling, not `FileSystemWatcher`**: per-source cycle with a configurable interval. `FileSystemWatcher` loses events on SMB and does not report drops. A watcher may be added later as a local optimization.
- **File identity**: record (size, position, timestamp and, where available, file id/inode) to tell rotation from truncation. On SMB the id may be unreliable, so it is combined with the "size smaller than position" check.
- **Reading**: `FileStream` with `FileShare.ReadWrite | Delete`, seek to position and bounded batch reads.
- **Flow**: each source publishes lines into a `Channel<LogLine>`; consumers (viewer, rules) subscribe. Decouples producers from consumers.
- **Isolation**: one task per source; a failure in one does not affect others.
- **Position persistence**: saved in batches and on shutdown, not per line.

## Risks / Trade-offs

- [Maximum delay equals the polling interval] → low default interval, configurable.
- [Imprecise rotation detection on SMB] → combine identity and size; cover with rotation tests.
- [Lines lost if the file rotates between polls] → read the previous file by identity when possible; document the limit.
