## Context

Lines arrive from the tailing channel. Alerts must be produced independently from the UI and from how they are delivered. See `proposal.md`. Depends on `core-log-tailing` and `local-persistence`.

## Goals / Non-Goals

**Goals:**
- A testable rule engine in `Core` with no UI or OS dependency.
- Throttling applied once per event, before any delivery channel.

**Non-Goals:**
- Threshold rules ("N errors in M minutes") and absence detection.
- Format-aware parsing (JSON, Serilog, etc.).
- Delivery (handled in `notification-channels`).

## Decisions

- **Regex**: .NET `Regex` with `RegexOptions.Compiled` where it pays off and a match timeout to bound evaluation. Rejected: hand-rolled matching.
- **Pipeline**: `LogLine` → `RuleEngine` → `AlertEvent` → `Throttler` → delivery. The throttler sits before channels so every channel sees the same filtered stream.
- **Throttling**: per-rule token window with a configurable size and limit; a summary event is emitted when the window closes with suppressed items.
- **History**: every match is stored with a `suppressed` flag; retention by age and/or count, applied periodically.
- **Hot reload**: rule changes take effect on the next line without restarting sources.

## Risks / Trade-offs

- [A badly written regex slows evaluation] → match timeout and per-rule error reporting.
- [History grows quickly on noisy logs] → retention plus storing a bounded line length.
- [Summary alert arrives late] → window size is configurable; default is short.
