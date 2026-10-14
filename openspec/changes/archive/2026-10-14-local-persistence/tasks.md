## 1. Project and schema

- [x] 1.1 Create `src/MothEidol.Persistence` with `Microsoft.Data.Sqlite`
- [x] 1.2 Resolve the data directory per platform
- [x] 1.3 Migration engine based on `PRAGMA user_version` and initial migration

## 2. Settings

- [x] 2.1 Settings interface and repository with default values
- [x] 2.2 Move the start-with-session option to persisted settings

## 3. Resilience

- [x] 3.1 Detect a corrupt database, back it up and recreate
- [x] 3.2 Detect a newer schema and warn without modifying
- [x] 3.3 Show the warning to the user in the UI

## 4. Verification

- [x] 4.1 Tests for migration, settings and recovery using temporary databases
- [x] 4.2 Run on macOS: change a setting, restart and check it persists
- [x] 4.3 Close the change: archive, bump the version (semver), commit, push and tag it
