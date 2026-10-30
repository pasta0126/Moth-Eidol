## Context

The core must be independent from the UI and the platform, and work with local files and SMB paths. See `proposal.md`. Depends on `local-persistence` for sources and positions.

## Goals / Non-Goals

**Goals:**
- Reliable tail across rotation, truncation and network drops.
- A core testable with temporary files, without a UI.

**Non-Goals:**
- Multi-line grouping (stack traces as a single event) and format-specific parsers: delivered in `serilog-parsing`.
- Recursive folder scanning or multiple patterns per source.

## Decisions

- **Polling, not `FileSystemWatcher`**: per-source cycle with a configurable interval. `FileSystemWatcher` loses events on SMB and does not report drops. A watcher may be added later as a local optimization.
- **File identity**: a fingerprint (hash of the file's first 128 bytes) plus the read position, instead of platform file ids, which are unreliable on SMB and not portable in .NET. A different fingerprint means the file at the path was replaced or rotated; a size smaller than the position with the same fingerprint means truncation. After a rename rotation the previous file is found by scanning the newest sibling files for the stored fingerprint.
- **Reading**: `FileStream` with `FileShare.ReadWrite | Delete`, seek to position and bounded batch reads.
- **Folder sources**: a source is either a file path or a folder plus a file-name glob. Each polling cycle resolves the active file as the newest match (by last write time, ties broken by name). When a newer file appears, the previous one is read to its end and the new one is read from its beginning. Serilog rolls daily files by creating a new name, not by renaming, so this is the main rotation case.
- **Flow**: each source publishes lines into a `Channel<LogLine>`; consumers (viewer, rules) subscribe. Decouples producers from consumers.
- **Isolation**: one task per source; a failure in one does not affect others.
- **Position persistence**: saved in batches and on shutdown, not per line.

## Risks / Trade-offs

- [Maximum delay equals the polling interval] → low default interval, configurable.
- [Imprecise rotation detection on SMB] → combine fingerprint and size; cover with rotation tests.
- [A replacement file that starts with the same first bytes as the old one is not detected as a rotation unless it is also shorter than the read position] → log files start with timestamps, so this is rare; documented limit.
- [A single line longer than 1 MB is emitted in pieces] → bounded memory wins over exactness for pathological lines.
- [Newest-by-write-time can pick a stale file if clocks differ across a share] → compare file names as a tie-breaker and cover with tests.
- [Lines lost if the file rotates between polls] → read the previous file by identity when possible; document the limit.
