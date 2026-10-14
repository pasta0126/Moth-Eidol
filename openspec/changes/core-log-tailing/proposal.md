## Why

This is the core of the product: reading local or network-share log files live and showing them, reliably across rotations, disconnections and restarts.

## What Changes

- Log source management (local files or network paths accessible with the OS user).
- Live incremental reading via polling, handling rotation, truncation and unavailability.
- Persisted read position so lines are not repeated after a restart.
- Live viewer with auto-scroll and pause.

## Capabilities

### New Capabilities
- `log-sources`: add, remove, enable and status of log sources.
- `log-tailing`: reliable incremental file reading, independent from the UI.
- `live-log-viewer`: live display of a source's lines.

### Modified Capabilities

## Impact

- New code in `MothEidol.Core` (tailing) and views in `MothEidol.App`.
- Uses `local-persistence` for sources and read positions.
- Enables `alert-rules`, which consumes the emitted lines.
