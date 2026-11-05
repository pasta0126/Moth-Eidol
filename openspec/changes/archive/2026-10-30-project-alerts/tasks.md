## 1. Alert production

- [x] 1.1 `AlertEvent` model and in-process event stream
- [x] 1.2 `AlertEvaluator` applying level rules and the project's warning setting
- [x] 1.3 Message normalizer and group key (normalized first line plus first exception type)

## 2. Event groups

- [x] 2.1 Event store: groups with count, first/last time and bounded recent occurrences, with migration
- [x] 2.2 Pending/seen state: mark one group or all of a project as seen; new occurrence makes it pending
- [x] 2.3 Retention (seen and quiet groups by age, occurrences beyond the cap)

## 3. Frequency policies

- [x] 3.1 Immediate-with-cooldown policy with suppressed count and summary on cooldown end
- [x] 3.2 Periodic-summary policy with counts per level and sample entries
- [x] 3.3 Clock abstraction and per-project policy state, reconfigurable on project change

## 4. Screens

- [x] 4.1 Events section in the project screen as the landing section: groups with pending first, filter pending/all
- [x] 4.2 Group detail with the recent occurrences and their full text, and mark as seen
- [x] 4.3 Dashboard tiles with pending count and worst pending level

## 5. Verification

- [x] 5.1 Tests: level matrix, unknown level, normalization, grouping, pending/seen, re-pending, retention, cooldown, summary, empty interval, hot reconfiguration
- [x] 5.2 Run on macOS with the demo projects and check tiles, events, detail and seen
- [x] 5.3 Close the change: archive, bump the version (semver), commit, push and tag it
