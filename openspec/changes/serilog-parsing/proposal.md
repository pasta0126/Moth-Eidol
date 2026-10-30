## Why

Raw lines are not enough to decide what deserves an alert. The real Serilog logs carry a timestamp and a level on each entry, and exceptions span many lines (up to 400+ continuation lines per file), so the unit of monitoring must be the entry, not the physical line.

## What Changes

- A `LogEntry` model with `Timestamp`, `Level` and `Message` (including continuation lines).
- Grouping of continuation lines (stack traces) under the entry header that precedes them.
- A pluggable entry parser with a Serilog text parser by default.
- Live viewer shows entries colored by level and can filter by level.

## Capabilities

### New Capabilities
- `log-entries`: parsing of raw lines into structured entries with multi-line grouping.

### Modified Capabilities
- `live-log-viewer`: shows entries, colored by level, with a level filter.

## Impact

- New code in `MothEidol.Core` (parser, entry assembler) and changes to the viewer in `MothEidol.App`.
- Consumes the line channel from `core-log-tailing`; produces an entry channel consumed by `project-alerts`.
- Sample logs in `logs/` (git-ignored) are the reference corpus; anonymized subsets go under `tests/`.
