## Why

The version footer sits flush against the content above it, and the "Word wrap" checkbox lives in the middle of the tail toolbar (and at the left of the event detail header), far from the text it affects. Actions that act on the shown text, such as word wrap and "Open folder" (and soon Raw and Refresh), belong together next to that text.

## What Changes

- The footer gets a small space above it so it no longer touches the content.
- A small reusable **view actions bar** is placed at the top-right corner of the tail entry list and of the event detail. It holds the actions that act on that text:
  - tail: Word wrap, Open folder;
  - event detail: Word wrap.
- Those actions leave the tail toolbar and the detail header; the toolbar keeps what filters or controls the stream (source, level, pause auto-scroll, clear, max lines, scroll state).
- The bar is built so new actions (for example the Raw toggle and Refresh from `raw-text-view`) are added to it without redesign.
- Behavior of the actions does not change; the shared word wrap choice stays shared.

## Capabilities

### New Capabilities

(none)

### Modified Capabilities
- `app-shell`: the footer is visually separated from the content.
- `live-log-viewer`: word wrap and its placement in the view actions bar of the tail.
- `event-insights`: word wrap option placement in the view actions bar of the event detail.
- `project-screen`: the open-folder action of the tail is in the view actions bar.

## Impact

- App: new `ViewActionsBar` control; `TailView.axaml`, `EventsView.axaml` and `MainWindow.axaml` layout changes.
- No core, persistence or dependency changes. Low risk, visual only.
