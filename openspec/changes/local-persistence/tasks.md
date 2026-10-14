## 1. Project and schema

- [ ] 1.1 Create `src/MothEidol.Persistence` with `Microsoft.Data.Sqlite`
- [ ] 1.2 Resolve the data directory per platform
- [ ] 1.3 Migration engine based on `PRAGMA user_version` and initial migration

## 2. Settings

- [ ] 2.1 Settings interface and repository with default values
- [ ] 2.2 Move the start-with-session option to persisted settings

## 3. Resilience

- [ ] 3.1 Detect a corrupt database, back it up and recreate
- [ ] 3.2 Detect a newer schema and warn without modifying
- [ ] 3.3 Show the warning to the user in the UI

## 4. Verification

- [ ] 4.1 Tests for migration, settings and recovery using temporary databases
- [ ] 4.2 Run on macOS: change a setting, restart and check it persists
- [ ] 4.3 Close the change: archive, bump version to 0.2.0, commit, push and tag `v0.2.0`
