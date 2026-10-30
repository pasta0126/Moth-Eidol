## 1. Analysis

- [ ] 1.1 `StackTraceParser`: exception headers, frames with location, inner exceptions, application vs framework frames
- [ ] 1.2 Title and location derivation for a group occurrence
- [ ] 1.3 Relative time formatter

## 2. Time series

- [ ] 2.1 Hourly buckets per group: storage, migration, batched increments, retention
- [ ] 2.2 Sparkline series per group and per project (last 24 hours)
- [ ] 2.3 Trend: last 24 hours versus the previous 24 hours
- [ ] 2.4 New and Regressed flags on groups

## 3. Context

- [ ] 3.1 Ring of the latest entries per source in the entry pipeline
- [ ] 3.2 Save the preceding entries with each stored occurrence (bounded)

## 4. Screens

- [ ] 4.1 Event rows: title, location, badges, sparkline, count and relative last time
- [ ] 4.2 Dashboard tiles: sparkline and health summary with trend
- [ ] 4.3 Event detail: ordered stack trace with collapsible framework frames
- [ ] 4.4 Event detail: context before the error and occurrence timeline with range selector
- [ ] 4.5 Copy summary action

## 5. Verification

- [ ] 5.1 Tests: parser on the fixtures, title and location, relative time, buckets, trend, flags, context capture, summary text
- [ ] 5.2 Run on macOS with the demo projects and check rows, tiles, detail, context, chart and copy
- [ ] 5.3 Close the change: archive, bump the version (semver), commit, push and tag it
