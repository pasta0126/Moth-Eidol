## 1. Core

- [x] 1.1 `RawLogText`: plain log lines for a list of entries (time, level, text, continuation lines), with the 20,000-line cap and truncation flag

## 2. Tail

- [x] 2.1 "Raw" toggle and "Refresh" button in the tail toolbar
- [x] 2.2 Read-only monospace `TextBox` shown in place of the list, snapshot on enable, wrap bound to the shared word wrap option, truncation note
- [x] 2.3 Pause auto-scroll while Raw is on and restore the styled live view when it is turned off

## 3. Event detail

- [x] 3.1 "Raw" toggle in the event detail showing `OccurrenceCopyText.Build` in a read-only `TextBox`; follows the selected occurrence and the wrap option
- [x] 3.2 Copy button in each Occurrences list item with confirmation, without changing the selected occurrence

## 4. Validation

- [x] 4.1 Unit tests: `RawLogText` (order, continuation lines, level filter input, cap and truncation flag), copy text equals the Raw text of the detail
- [x] 4.2 Build and run the tests
- [ ] 4.3 Check manually: Raw in tail and detail, drag/double/triple click/Shift+click/select all, wrap on/off, snapshot with arriving entries, Refresh, copy button (deferred to the maintainer)
