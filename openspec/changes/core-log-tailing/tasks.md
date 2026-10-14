## 1. Model and sources

- [ ] 1.1 Log source model and repository on top of `local-persistence`
- [ ] 1.2 Source management (add, remove, enable) and statuses
- [ ] 1.3 Sources screen in the application

## 2. Tailing

- [ ] 2.1 Incremental reader with polling, position and batch reads
- [ ] 2.2 Truncation and rotation detection
- [ ] 2.3 Unavailability handling with retries and recovery
- [ ] 2.4 Complete lines, UTF-8/UTF-16 with BOM, CRLF/LF
- [ ] 2.5 Persistence and resume of the read position
- [ ] 2.6 Line channel for consumers

## 3. Viewer

- [ ] 3.1 Live view with auto-scroll and pause
- [ ] 3.2 Line limit in the view

## 4. Verification

- [ ] 4.1 Tests with temporary files: append, truncation, rotation, disappearance, restart, partial line
- [ ] 4.2 Run on macOS against a local file and, if possible, a shared folder
- [ ] 4.3 Close the change: archive, bump version to 0.3.0, commit, push and tag `v0.3.0`
