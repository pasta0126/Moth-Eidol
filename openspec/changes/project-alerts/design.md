## Context

Alerts are produced independently from the UI and from how they are delivered. Whether an entry is alert-level depends only on its level and the project's configuration. Real logs repeat the same error hundreds of times, so the unit the user reviews is a group, not an occurrence. See `proposal.md`. Depends on `serilog-parsing`, `projects` and `local-persistence`.

## Goals / Non-Goals

**Goals:**
- A testable alert pipeline in `Core` with no UI or OS dependency.
- A review workflow (pending, detail, seen) that stays usable with very repetitive errors.
- The frequency policy applied once per event, before any delivery channel.

**Non-Goals:**
- User-defined regex rules, threshold rules ("N errors in M minutes") and absence detection (backlog).
- Delivery (handled in `notification-channels`).
- Sharing events or seen state across users.
- A flat per-occurrence list view (groups only; occurrences are visible inside a group).

## Decisions

- **Pipeline**: `LogEntry` → `AlertEvaluator` (level + project config) → `AlertEvent` → (a) `EventStore` grouping and (b) project `FrequencyPolicy` → delivery. Grouping records every alert-level entry regardless of delivery throttling.
- **Group key**: project + level + normalized first line of the entry message + the exception type of the first exception line when the entry has a stack trace. Normalization replaces GUIDs, ISO timestamps, quoted values and digit runs with placeholders, so "Envío KO ... registro 164568" and "... registro 164570" fall in the same group, while the same message with a different exception type does not.
- **Group state**: `Count`, `FirstSeen`, `LastSeen`, `SeenAt` (when the user last marked it), and `Pending` = occurrences after `SeenAt`. A new occurrence after being marked seen makes the group pending again.
- **Occurrences**: only the most recent N per group are stored with their full text (including stack trace, bounded length); the group count keeps counting beyond N.
- **Seen actions**: mark one group, or all groups of a project, as seen.
- **Tile status**: derived from the project's pending groups: worst pending level (error over warning) and pending occurrence count; no pending means "clear". Source health from `projects` still applies.
- **Immediate with cooldown**: first delivery at once, further ones counted and summarized when the cooldown ends. **Periodic summary**: one aggregated delivery per interval if there were alert-level entries.
- **Policy state per project**, in memory, with a clock abstraction for deterministic tests.
- **Retention**: groups seen and quiet for longer than the configured age are removed, and occurrences beyond N are trimmed; pending groups are never removed automatically.
- **Level model reuse**: levels come from `serilog-parsing`; entries with unknown level never alert.

## Risks / Trade-offs

- [Normalization merges errors that differ only by meaningful numbers] → the group detail lists the individual occurrences with full text.
- [Normalization splits one error into many groups when messages embed free text] → the key uses only the first line; refine rules from real samples.
- [Deliveries arrive late in summary mode] → interval and cooldown are configurable per project.
- [Application closed during a summary window] → pending deliveries are lost; groups still record every alert-level entry.
- [Storage grows on noisy logs] → bounded occurrences per group and age-based retention.
