## 1. Core: draft and validation

- [x] 1.1 Add `ProjectDraft` with name, source list and `AlertSettings`, and per-step validation returning user-readable messages
- [x] 1.2 Unit tests: empty/duplicate name, no sources, duplicate source, minutes out of range

## 2. Core: location inspector

- [x] 2.1 Add `LocationInspector` returning status, matching files, newest file and a bounded sample of recent parsed entries with levels
- [x] 2.2 Classify missing/unreadable as errors and no-match/unrecognized-format as warnings
- [x] 2.3 Unit tests with temp folders: file, folder + pattern, missing, no matches, unrecognized format, UTF-16/CRLF, large file reads only the tail

## 3. Atomic creation

- [x] 3.1 Add `ProjectService.CreateWithSources` that validates first and saves project, sources and alerts, undoing what was saved if a step fails
- [x] 3.2 Tests in Core and Persistence: success, duplicate name leaves nothing saved, failure rolls back

## 4. Wizard UI

- [x] 4.1 Add `ProjectWizardView` with step indicator and Back/Next/Cancel; steps name, location, alerts, review
- [x] 4.2 Location step: file/folder choice, pattern, browse, live check with async inspection and stale-result discard, sources list with remove
- [x] 4.3 Alerts step with plain-language explanations; review step listing all values
- [x] 4.4 Dashboard "New project" tile launches the wizard (remove inline name panel); on finish open the project on events; cancel returns to the dashboard
- [x] 4.5 Keep the version footer visible; use theme brushes only

## 5. Verification and release

- [x] 5.1 `dotnet test` and `openspec validate --all` pass
- [x] 5.2 Maintainer runs the app and walks the wizard (file, folder, bad path, cancel)
- [x] 5.3 Archive the change, bump the version, update AGENTS.md UI structure, commit, push and tag
