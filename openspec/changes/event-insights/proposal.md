## Why

A list of raw log lines is written for machines. People triaging issues need what Sentry gives: a readable title and where it happens, how recent and how frequent it is, whether it is new or came back, the stack trace in order, what happened just before, and a way to hand the finding to someone else. The grouping and pending/seen workflow from `project-alerts` stays; this change makes it human-friendly.

## What Changes

- **Readable title and location** for each event group: exception type and message as the title and the most relevant application frame as the location; the message line when there is no exception.
- **Relative times** ("3 min ago") with the exact time available, and first seen.
- **Frequency sparkline** (last 24 hours) on each group and on each dashboard tile.
- **Badges** New (first seen in the last 24 hours) and Regressed (came back after being marked as seen).
- **Tile health summary**: events in the last 24 hours and the trend against the 24 hours before.
- **Event detail**: the stack trace as an ordered list of frames with application code highlighted and framework frames collapsed; the entries that preceded the error from the same source (context); an occurrence timeline chart; and a button to copy a text summary.

## Capabilities

### New Capabilities
- `event-insights`: human-oriented presentation and analysis of event groups.

### Modified Capabilities

## Impact

- New code in `MothEidol.Core` (stack trace parser, title/location derivation, relative time formatter, time buckets, context capture) and in `MothEidol.App` (event rows, sparkline and chart controls, detail view, tile additions).
- Extends the event store of `project-alerts` with hourly buckets and per-occurrence context (migration).
- No new dependencies: sparkline and chart are drawn with Avalonia primitives.
