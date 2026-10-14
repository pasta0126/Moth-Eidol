## Why

Monitoring needs to remember sources, rules, read positions and history between runs. Without local persistence every restart would lose configuration and re-alert on lines already seen. This is version `0.2.0`.

## What Changes

- Local per-user SQLite database, nothing distributed.
- Versioned schema with automatic migrations at startup.
- Application settings store.
- Tolerance to a corrupt or inaccessible database.

## Capabilities

### New Capabilities
- `local-persistence`: local SQLite storage, migrations, settings and corruption recovery.

### Modified Capabilities

## Impact

- New dependency: SQLite provider for .NET.
- Later changes (`core-log-tailing`, `alert-rules`, `notification-channels`) store their state here.
