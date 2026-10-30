## 1. Model and parser

- [ ] 1.1 `LogEntry` and level model
- [ ] 1.2 `ILogEntryParser` and `SerilogTextParser`
- [ ] 1.3 Entry assembler: header detection, continuation grouping, idle flush, max length
- [ ] 1.4 Entry channel fed from the line channel

## 2. Viewer

- [ ] 2.1 Show entries instead of raw lines, with level colors using `Moth*Brush` resources
- [ ] 2.2 Level filter in the viewer
- [ ] 2.3 Expand/collapse long multi-line entries

## 3. Verification

- [ ] 3.1 Anonymized fixtures derived from `logs/` (one per project style) under `tests/`
- [ ] 3.2 Tests: header parsing, offsets, all levels, stack traces, emojis, very long lines, idle flush, unknown level
- [ ] 3.3 Run on macOS against a growing file and check grouping and filter
- [ ] 3.4 Close the change: archive, bump the version (semver), commit, push and tag it
