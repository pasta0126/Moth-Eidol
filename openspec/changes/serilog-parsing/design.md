## Context

Observed format in the real logs: `2026-10-28 10:49:09.258 +01:00 [INF] message`, levels `INF`, `WRN`, `ERR` (and Serilog's `VRB`, `DBG`, `FTL`), UTF-8 with CRLF, long lines up to ~4000 characters, emojis in messages. Stack traces follow an `[ERR]` header as lines without a timestamp. See `proposal.md`. Depends on `core-log-tailing`.

## Goals / Non-Goals

**Goals:**
- Turn raw lines into entries without losing or reordering any text.
- Keep the parser replaceable so other formats can be added later.

**Non-Goals:**
- JSON/structured sinks and custom output templates (only the default Serilog text layout for now).
- Alerting decisions (handled in `project-alerts`).

## Decisions

- **Entry boundary**: a line that matches the header pattern (`timestamp +offset [LVL] `) starts a new entry; any other line is appended to the current entry. Lines before the first header form an entry with unknown level.
- **Flush on completion**: an entry is emitted when the next header arrives or after a short idle timeout, so the last entry of a burst is not held back indefinitely.
- **Parser abstraction**: `ILogEntryParser` in `Core`; `SerilogTextParser` is the default. Rejected: a regex configured by the user (out of scope, backlog).
- **Level model**: normalized enum (Verbose, Debug, Information, Warning, Error, Fatal, Unknown) mapped from `VRB/DBG/INF/WRN/ERR/FTL`.
- **Bounded size**: a maximum entry length (configurable) truncates pathological stack traces while keeping the header.
- **Timestamp**: parsed with offset into `DateTimeOffset`; if parsing fails the entry keeps `Unknown` timestamp and is still emitted.

## Risks / Trade-offs

- [A stack trace line that looks like a header] → require the full timestamp + offset + bracketed level shape.
- [Entry delayed by the idle timeout] → keep the timeout short and configurable.
- [Unknown level tokens] → map to `Unknown`, never drop the entry.
