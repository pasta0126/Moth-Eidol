## Context

Alerts are produced independently from the UI and from how they are delivered. The alert decision depends only on the entry level and the project's configuration. See `proposal.md`. Depends on `serilog-parsing`, `projects` and `local-persistence`.

## Goals / Non-Goals

**Goals:**
- A testable alert pipeline in `Core` with no UI or OS dependency.
- The frequency policy applied once per event, before any delivery channel.

**Non-Goals:**
- User-defined regex rules, threshold rules ("N errors in M minutes") and absence detection (backlog).
- Delivery (handled in `notification-channels`).
- Sharing alerts across users.

## Decisions

- **Pipeline**: `LogEntry` → `AlertEvaluator` (level + project config) → `AlertEvent` → project `FrequencyPolicy` → delivery. The policy sits before channels so every channel sees the same filtered stream.
- **Immediate with cooldown**: the first alert in a quiet period is delivered at once; further alerts within the cooldown are counted and a single summary ("+N more") is delivered when the cooldown ends.
- **Periodic summary**: alerts accumulate and one summary per interval is delivered (counts per level and the first entries as a sample); nothing is delivered if there were none.
- **Policy state per project**, in memory, with clock abstraction for deterministic tests.
- **History**: every alert-level entry is stored with a `suppressed` flag (not delivered individually) and a bounded message length; retention by age and/or count, applied periodically.
- **Level model reuse**: levels come from `serilog-parsing`; entries with unknown level never alert.

## Risks / Trade-offs

- [Summary alerts arrive late] → interval and cooldown are configurable per project, with short defaults for immediate mode.
- [History grows quickly on noisy logs] → retention plus bounded stored text.
- [Application closed during a summary window] → pending summaries are lost; history still records every alert.
