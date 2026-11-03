## Why

In the tail and in the event detail each line (or text fragment) is its own control, so selecting across several lines with the mouse is awkward: dragging over text never spans lines. Rebuilding the viewers as an editor-like control is large and risky. A plain-text mode is much simpler and gives people exactly what they need: select and copy whatever they want. Copying a whole occurrence (context before the error plus message) also needs a one-click action.

## What Changes

- The tail and the event detail keep their current styled view and gain a **Raw** toggle button. In Raw mode the content is shown as plain text, without styles, in a read-only text area where the native text editing behavior works: drag across lines, double click a word, triple click a line, Shift+click, select all and copy.
- Tail Raw shows the entries currently visible after the level filter, as plain log lines (time, level, text, with continuation lines). It is a snapshot taken when Raw is switched on, so the selection is not disturbed while new entries arrive; a Refresh action takes a new snapshot and switching Raw off returns to the live view.
- Event detail Raw shows the same text as "Copy all": the context entries followed by the occurrence text.
- Raw follows the word wrap option.
- Each item of the Occurrences list gets a **Copy** button that copies the whole detail of that occurrence (context entries before the error plus the full message and stack trace).
- The styled view, its level coloring, collapsing and context menus do not change.

## Capabilities

### New Capabilities

(none)

### Modified Capabilities
- `live-log-viewer`: adds the Raw view of the tail.
- `event-insights`: adds the Raw view of the event detail and the Copy button per occurrence.

## Impact

- App: a Raw toggle in the tail toolbar and in the event detail, a read-only `TextBox` shown in place of the styled view, a Copy button in the Occurrences item template.
- Core: a small pure formatter for the Raw tail text (plain log lines); `OccurrenceCopyText` reused for the detail.
- No new dependencies, no persistence changes. Low risk.
