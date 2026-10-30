## 1. Channel infrastructure

- [x] 1.1 `INotifier` abstraction and a dispatcher that fans out and isolates failures
- [x] 1.2 Per-channel enable settings persisted via `local-persistence`
- [x] 1.3 Channels settings screen

## 2. Built-in channels

- [x] 2.1 In-app toast window with stacking, cap and auto-dismiss
- [x] 2.2 Tray icon alert state and unread counter with acknowledge
- [x] 2.3 Sound channel behind an audio interface

## 3. Verification

- [x] 3.1 Tests for the dispatcher (isolation, enable/disable) and counter logic with test doubles
- [ ] 3.2 Run on macOS: trigger a rule with the window hidden and check toast, tray counter and sound (deferred: the maintainer could not view the app; the sound channel was verified end to end in the running app, where afplay was started for each delivered alert. Toasts and the tray badge were not seen)
- [ ] 3.3 Close the change: archive, bump the version (semver), commit, push and tag it
