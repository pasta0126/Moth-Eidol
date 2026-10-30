## 1. Alert production

- [ ] 1.1 `AlertEvent` model and in-process event stream
- [ ] 1.2 `AlertEvaluator` applying level rules and the project's warning setting

## 2. Frequency policies

- [ ] 2.1 Immediate-with-cooldown policy with suppressed count and summary on cooldown end
- [ ] 2.2 Periodic-summary policy with counts per level and sample entries
- [ ] 2.3 Clock abstraction and per-project policy state, reconfigurable on project change

## 3. History

- [ ] 3.1 Alert history storage with suppressed flag, migration and retention
- [ ] 3.2 History view (newest first, filter by project and level)

## 4. Verification

- [ ] 4.1 Tests: level matrix, unknown level, cooldown, summary, empty summary interval, hot reconfiguration, retention
- [ ] 4.2 Run on macOS: replay copies of the sample logs into a monitored folder and check events and history
- [ ] 4.3 Close the change: archive, bump the version (semver), commit, push and tag it
