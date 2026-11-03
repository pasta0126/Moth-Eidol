## Why

Tail lines are rendered on a single line with a horizontal scrollbar, so long messages run off-screen and are hard to read. Users want lines to wrap to the window width.

## What Changes

- A "Word wrap" checkbox in the Tail toolbar, enabled by default.
- When enabled, entries wrap to the available width (continuation lines and stack traces included) and the horizontal scrollbar is hidden.
- When disabled, the current behavior returns: no wrapping, horizontal scrolling.
- The same option and setting also govern the event detail's tail-like parts: the context entries before the error and the occurrence text with its stack trace.
- The choice is remembered across runs (application-wide).

## Capabilities

### Modified Capabilities
- `live-log-viewer`: wrapping option for the tail.
- `event-insights`: context and occurrence text in the event detail follow the wrapping option.

## Impact

- `TailView` and the event detail in `EventsView` (toolbar checkbox, row template wrapping), plus one persisted setting. No new dependencies.
