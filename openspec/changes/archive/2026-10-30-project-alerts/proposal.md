## Why

Monitoring is only useful if the team sees what needs attention without being flooded. Alert-level entries must be collected per project, grouped so a repeating error is one item, and kept as pending until the user has reviewed them. The dashboard then tells at a glance which projects have pending issues.

## What Changes

- Evaluation of every log entry against its project's alert configuration (information never alerts, warning depends on the project, error and above always alert).
- **Event groups**: alert-level entries with the same level and the same normalized message form one group with a count, first and last time, and the most recent occurrences with their full text.
- **Pending and seen**: each group has a pending count; the user can mark a group, or all of a project, as seen, and a new occurrence makes the group pending again.
- **Events section** in the project screen (the landing section): groups listed with pending first, opened for detail, marked as seen.
- **Tile status**: the dashboard tiles show the pending count and the worst pending level.
- `AlertEvent`s carrying project, source, entry, level and time, for `notification-channels`.
- Per-project frequency policy for deliveries: immediate with cooldown, or periodic summary, with aggregated counts.
- Persisted groups and occurrences with retention.

## Capabilities

### New Capabilities
- `project-alerts`: alert-level detection per project, event groups, pending/seen state and the events section.
- `alert-throttling`: per-project cooldown and periodic summary for deliveries.

### Modified Capabilities
- `project-dashboard`: tiles show pending counts and level.
- `project-screen`: the events section becomes the landing section.

## Impact

- New code in `MothEidol.Core` (alert evaluator, message normalizer, grouping, frequency policies) and the events section and tile status in `MothEidol.App`.
- Consumes entries from `serilog-parsing` and project configuration and screens from `projects`; stores groups and occurrences via `local-persistence`.
- Produces `AlertEvent`s consumed by `notification-channels`.
- Replaces the earlier `alert-rules` plan: user-defined regex rules are in the backlog.
