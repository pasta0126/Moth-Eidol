## 1. Rule engine

- [ ] 1.1 Rule model, validation and repository on `local-persistence`
- [ ] 1.2 Rule engine evaluating lines with scope, case sensitivity and match timeout
- [ ] 1.3 `AlertEvent` model and in-process event stream

## 2. Throttling

- [ ] 2.1 Per-rule windowed rate limiter with global defaults and per-rule overrides
- [ ] 2.2 Summary alert on window close
- [ ] 2.3 Option to disable throttling globally or per rule

## 3. History

- [ ] 3.1 Alert history storage with suppressed flag and retention
- [ ] 3.2 Rules management screen
- [ ] 3.3 History view

## 4. Verification

- [ ] 4.1 Tests for matching, invalid regex, timeout, scope, throttling, summary and retention
- [ ] 4.2 Run on macOS: add a rule, append matching lines to a file and check events and history
- [ ] 4.3 Close the change: archive, bump version to 0.4.0, commit, push and tag `v0.4.0`
