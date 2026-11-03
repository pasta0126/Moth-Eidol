## Context

Avalonia `TextBlock` is not selectable; `SelectableTextBlock` is. The tail uses a `ListBox` of `TextBlock` rows with a tap handler (`OnEntryTapped`), and tiles/event rows use tap or click to navigate.

## Decisions

- Use `SelectableTextBlock` for read-only texts, applied through a global style in `MothTheme.axaml` where possible, and explicit replacement in templates otherwise. Rationale: no new dependency and consistent look.
- Tail: selection inside one row works with `SelectableTextBlock`. Selecting across rows is not supported by per-row controls, so add a context menu "Copy" and "Copy all visible" on the list (copies the text of the selected rows, or the visible filtered rows), plus Cmd/Ctrl+C on selected rows. Rejected: a single big text control, because it breaks level coloring, filtering and bounded memory.
- Navigation tiles/rows: keep click-to-open on the container; text selection by drag must not trigger navigation (open only on a click without drag).
- Brushes stay `Moth*Brush`; selection brush comes from the theme (amber on obsidian, readable).

## Risks

- Drag vs click conflicts on tiles: verify manually on macOS and Windows.
- Virtualization in the tail: selection must not force realizing all rows.
