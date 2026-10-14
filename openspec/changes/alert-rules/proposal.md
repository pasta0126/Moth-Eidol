## Why

Monitoring is only useful if it detects what matters. Users need to define regex rules over log lines and get alert events, without being flooded when the same error repeats.

## What Changes

- Regex rules that evaluate every emitted line, per source or global.
- Alert events carrying source, line, rule and time.
- Configurable anti-spam throttling with aggregated summaries.
- Persisted rules and alert history with retention, plus a history view.

## Capabilities

### New Capabilities
- `alert-rules`: rule definition, evaluation and alert events.
- `alert-throttling`: configurable rate limiting and aggregation of alerts.

### Modified Capabilities

## Impact

- New code in `MothEidol.Core` (rule engine, throttling) and rules/history views in `MothEidol.App`.
- Consumes lines from `core-log-tailing`; stores rules and history via `local-persistence`.
- Produces `AlertEvent`s consumed by `notification-channels`.
