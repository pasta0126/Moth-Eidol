## Why

Text shown by the application (tail lines, event titles, stack traces, occurrence details, project names, messages) cannot be selected or copied in most places, so users must retype or screenshot what they want to paste into a ticket or chat. A log monitor must let people copy what it shows.

## What Changes

- Read-only text is selectable and copyable everywhere in the application: tail lines, event groups and details, stack traces, context entries, timelines, dashboard tiles, project settings labels, wizard review, application settings, footer.
- Tail entries (including multi-line ones) can be selected across several lines and copied with the standard shortcut and a context menu "Copy".
- The tail-like parts of an event detail (context entries before the error, occurrence text and stack trace) are selectable across lines and copyable, with the same context menu as the tail.
- Clicking behavior is preserved: tiles and event rows still open on click; selection starts by dragging.
- Editable inputs keep their native copy/paste behavior.

## Capabilities

### New Capabilities
- `text-selection`: selectable and copyable text across all screens.

### Modified Capabilities
- `live-log-viewer`: tail lines are selectable and copyable.

## Impact

- Views in `src/MothEidol.App/Views/*.axaml` and the theme styles; no core, storage or dependency changes.
- Possible interaction conflicts with row click/tap handlers (see design).
