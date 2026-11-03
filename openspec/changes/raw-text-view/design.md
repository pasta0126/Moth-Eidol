## Context

The tail is a `ListBox` of `EntryItem`s and the occurrence detail is built by `OccurrenceDetailBuilder`; both use per-fragment `SelectableTextBlock`s, which cannot select across lines. Avalonia's read-only `TextBox` already provides editor selection behavior natively.

## Decisions

- Raw is a mode of the existing views, not a new screen: a toggle button switches the visibility of the styled view and of a read-only, monospace `TextBox` (`AcceptsReturn`, no caret editing, theme brushes for colors only as background/foreground of the box, never per-level colors).
- Tail Raw text comes from a pure formatter in Core (`RawLogText`): one entry after another, in display order, as `time level message` with the continuation lines unchanged, using the entries visible after the level filter. Testable without UI.
- Snapshot, not live: entering Raw captures the text once; new entries keep being monitored and shown in the styled view when Raw is turned off. A "Refresh" button re-captures. Rationale: setting `TextBox.Text` resets the selection and re-layouts, which would fight the user; auto-scroll is paused while in Raw.
- Size guard: the Raw snapshot is capped to the last 20,000 lines with a visible note when truncated, to keep the text box responsive; the styled view and "Copy all visible" are unaffected.
- Event detail Raw uses `OccurrenceCopyText.Build` as is, so Raw, "Copy all" and the per-occurrence Copy button always agree.
- Word wrap: the Raw `TextBox` binds `TextWrapping` to the existing shared word wrap option (wrap on: wrap; off: horizontal scrolling).
- The Raw toggle state is not persisted; each screen starts styled.
- Copy button per occurrence: a button in the Occurrences item template calling `OccurrenceCopyText.Build` for that occurrence, with the same "copied" confirmation as the summary copy; its click is handled so it neither selects the row nor changes the shown detail.
- The existing context menus and the styled selection stay untouched.

## Risks

- A snapshot may look stale while new entries arrive; the toolbar shows that Raw is a snapshot and offers Refresh.
- Very large buffers: mitigated by the 20,000-line cap.
