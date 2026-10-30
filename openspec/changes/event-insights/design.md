## Context

`project-alerts` produces grouped events with pending/seen state. The real logs are .NET Serilog output: exceptions appear as `Namespace.XxxException: message` followed by `   at Namespace.Class.Method(args) in C:\path\File.cs:line 60` frames, sometimes with inner exceptions. See `proposal.md`. Depends on `project-alerts`, `projects` and `serilog-parsing`.

## Goals / Non-Goals

**Goals:**
- Titles and locations a person understands without reading the raw entry.
- Cheap, bounded time-series data for sparklines, trends and the timeline.
- Context before an error, captured at the time it happens.

**Non-Goals:**
- Source code display, release tracking, assignees, comments and integrations.
- Parsing stack traces of other platforms (Java, Python); .NET format only, behind an interface so others can follow.
- Additional states beyond pending/seen.

## Decisions

- **Stack trace parsing**: `StackTraceParser` recognizes exception header lines (`Type: message`), `at` frames with optional `in path:line N`, inner exception separators and leaves unrecognized text untouched. Rejected: regex over the whole text per view; parse once when the occurrence is stored and keep the result.
- **Application vs framework frame**: a frame is application code when it has a source location (framework frames have none without symbols) or its namespace is not in a small framework prefix list (`System`, `Microsoft`, `Windows`, `Newtonsoft`, `Dapper`, `Polly`). Location of a group is the first application frame of the top exception.
- **Title**: the first exception line of the occurrence as `Type: message`; otherwise the first line of the entry message. The title shown is the most recent occurrence's text, so a changing id stays current; grouping still uses the normalized form.
- **Time series**: hourly buckets per group (`group_id`, `hour_start`, `count`) kept for at least 7 days; per-project series are sums over its groups. Sparkline = last 24 buckets; trend = last 24 hours versus the 24 before; timeline range selectable between 24 hours (hourly) and 7 days (daily).
- **Badges**: New when first seen within 24 hours; Regressed when a group has occurrences after its seen mark, cleared when marked seen again.
- **Context**: the pipeline keeps a small ring of the latest entries per source (default 10); when an alert-level occurrence is stored, the entries preceding it are saved with it (bounded length). Entries are the ones already assembled by `serilog-parsing`, so context entries are whole entries.
- **Relative time**: a pure formatter (`just now`, `N min ago`, `N h ago`, `yesterday`, then the date) with the exact local time as tooltip.
- **Copy summary**: plain text with project, title, location, level, counts, first and last seen, pending state and the latest occurrence's stack trace; copied through the Avalonia clipboard.
- **Drawing**: sparkline and chart are small custom controls built on Avalonia primitives using `Moth*Brush` resources; no charting package.

## Risks / Trade-offs

- [Stack trace formats vary between .NET versions and async state machines] → unrecognized lines are shown as plain text; tests use the real fixtures.
- [Framework prefix list misclassifies some libraries] → the source-location rule covers most cases; the list is a constant that is easy to extend.
- [Hourly buckets add writes on noisy logs] → increments are batched per flush interval, not per occurrence.
- [Context entries enlarge storage] → capped count and length per occurrence, and only for stored occurrences.
