## 1. Channel infrastructure

- [ ] 1.1 `INotifier` abstraction and a dispatcher that fans out and isolates failures
- [ ] 1.2 Per-channel enable settings persisted via `local-persistence`
- [ ] 1.3 Channels settings screen

## 2. Built-in channels

- [ ] 2.1 In-app toast window with stacking, cap and auto-dismiss
- [ ] 2.2 Tray icon alert state and unread counter with acknowledge
- [ ] 2.3 Sound channel behind an audio interface

## 3. Verification

- [ ] 3.1 Tests for the dispatcher (isolation, enable/disable) and counter logic with test doubles
- [ ] 3.2 Run on macOS: trigger a rule with the window hidden and check toast, tray counter and sound
- [ ] 3.3 Close the change: archive, bump version to 0.5.0, commit, push and tag `v0.5.0`
