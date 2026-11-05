## 1. Core

- [x] 1.1 Add the sound preference to `AppSettings` (`notify.sound.enabled`, platform-dependent default passed in) and make the toast default configurable
- [x] 1.2 Pure toast XML builder from `AlertText` (project, level, headline, silent flag) with escaping

## 2. App (Windows)

- [x] 2.1 `WindowsNotifier` behind `INotifier`, `[SupportedOSPlatform("windows")]`, registered only on Windows
- [x] 2.2 AUMID registration under HKCU for the unpackaged build; package identity for MSIX
- [x] 2.3 Click activation shows and focuses the main window
- [x] 2.4 Fallback to the in-app toast when the native notification fails; record the failure
- [x] 2.5 Do not register the sound channel on Windows; keep it on macOS/Linux driven by the same setting

## 3. Settings UI

- [x] 3.1 "Alert sound" switch (with sound / silent) in application settings, applied without restart
- [x] 3.2 Channel list shows the native channel on Windows and adjusts defaults

## 4. Validation

- [x] 4.1 Unit tests: XML builder (silent/sound, escaping), default resolution per platform, stored choice wins, setting persistence
- [x] 4.2 Build and run the tests
- [ ] 4.3 Check manually on Windows: native banner, Action Center entry, sound/silent, click opens the app, fallback (deferred to the maintainer)
