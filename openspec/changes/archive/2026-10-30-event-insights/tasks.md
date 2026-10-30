## 1. Analysis

- [x] 1.1 `StackTraceParser`: exception headers, frames with location, inner exceptions, application vs framework frames
- [x] 1.2 Title and location derivation for a group occurrence
- [x] 1.3 Relative time formatter

## 2. Time series

- [x] 2.1 Hourly buckets per group: storage, migration, increments in the occurrence's transaction, retention
- [x] 2.2 Sparkline series per group and per project (last 24 hours)
- [x] 2.3 Trend: last 24 hours versus the previous 24 hours
- [x] 2.4 New and Regressed flags on groups

## 3. Context

- [x] 3.1 Ring of the latest entries per source in the entry pipeline
- [x] 3.2 Save the preceding entries with each stored occurrence (bounded)

## 4. Screens

- [x] 4.1 Event rows: title, location, badges, sparkline, count and relative last time
- [x] 4.2 Dashboard tiles: sparkline and health summary with trend
- [x] 4.3 Event detail: ordered stack trace with collapsible framework frames
- [x] 4.4 Event detail: context before the error and occurrence timeline with range selector
- [x] 4.5 Copy summary action

## 5. Verification

- [x] 5.1 Tests: parser on the fixtures, title and location, relative time, buckets, trend, flags, context capture, summary text
- [ ] 5.2 Run on macOS with the demo projects and check rows, tiles, detail, context, chart and copy (deferred: the maintainer could not view the app; titles, locations, context and hourly counts were verified in the running app through its database)
- [ ] 5.3 Close the change: archive, bump the version (semver), commit, push and tag it
