## Context

The tail toolbar is a single `Grid` with the source, level, pause, word wrap, clear, max lines, open folder and scroll state. The event detail has its own `DetailWrapCheck` in the header grid. The footer is in `MainWindow.axaml`.

## Decisions

- `ViewActionsBar`: a small `UserControl` wrapping a right-aligned horizontal `StackPanel` with a compact spacing, exposing its content so each screen declares its own actions. It overlays nothing: it takes its own thin row at the top of the text area, aligned right, so it never covers text or scrollbars.
- Placement: tail: the row directly above the entry list, right-aligned; event detail: the row at the top of the detail column, right-aligned, above the title. Both are inside the area whose text they affect.
- Actions moved: tail Word wrap and Open folder; detail Word wrap. Their controls keep their names and handlers, so logic and the shared `WordWrapState` do not change.
- Footer: add a top margin (8 px) and a thin top border using `MothBorderBrush` so it reads as a separate strip; the footer stays visible on every screen (AGENTS.md).
- Order in the bar is stable (generic actions first, then specific ones) and the bar tolerates more items by wrapping onto a second line on narrow windows.
- When `raw-text-view` is applied afterwards, its Raw and Refresh buttons are added to this bar.

## Risks

- Narrow windows: the bar must not overlap the title or list; it keeps its own row and wraps.
