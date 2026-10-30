## 1. Model and persistence

- [ ] 1.1 `Project` and alert settings model with validation
- [ ] 1.2 Project repository on `local-persistence` (migration to add projects and link sources)
- [ ] 1.3 Project service: add, edit, enable, remove with hot reload of running sources

## 2. UI

- [ ] 2.1 Projects screen: list, add/edit form (name, folders, patterns, warning threshold, frequency mode)
- [ ] 2.2 Project status summary (sources state, last entry, recent counts)
- [ ] 2.3 Viewer selector by project and source

## 3. Verification

- [ ] 3.1 Tests: validation, persistence, migration, hot reload, removal stops sources
- [ ] 3.2 Run on macOS configuring projects against copies of the sample logs in `logs/`
- [ ] 3.3 Close the change: archive, bump the version (semver), commit, push and tag it
