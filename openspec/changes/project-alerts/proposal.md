## Why

Monitoring is only useful if the team hears about what matters without being flooded. Alerts are decided per project from the entry level and delivered with a per-project frequency policy, and every alert is kept in a history.

## What Changes

- Evaluation of every log entry against its project's alert configuration (information never alerts, warning depends on the project, error and above always alert).
- `AlertEvent`s carrying project, source, entry, level and time.
- Per-project frequency policy: immediate with cooldown, or periodic summary, with aggregated counts.
- Persisted alert history with retention and a history view.

## Capabilities

### New Capabilities
- `project-alerts`: level-based alert production per project and alert history.
- `alert-throttling`: per-project cooldown and periodic summary with aggregation.

### Modified Capabilities

## Impact

- New code in `MothEidol.Core` (alert evaluator, frequency policies) and a history view in `MothEidol.App`.
- Consumes entries from `serilog-parsing` and project configuration from `projects`; stores history via `local-persistence`.
- Produces `AlertEvent`s consumed by `notification-channels`.
- Replaces the earlier `alert-rules` plan: user-defined regex rules move to the backlog.
