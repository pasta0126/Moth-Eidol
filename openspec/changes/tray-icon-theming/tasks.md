## 1. Assets and detection

- [ ] 1.1 Create monochrome light/dark tray glyphs and the macOS template image, including alert-state variants
- [ ] 1.2 OS theme detection abstraction in `Platform/` with a change notification

## 2. Tray

- [ ] 2.1 Select the tray variant from the OS theme at startup and on change
- [ ] 2.2 macOS template image handling
- [ ] 2.3 Keep the unread counter and alert state legible in both variants

## 3. Validation

- [ ] 3.1 Tests for variant selection logic
- [ ] 3.2 Manual check: macOS light/dark, Windows light/dark taskbar, theme switch while running
- [ ] 3.3 Update `app-theme`/`app-shell` specs on archive and AGENTS.md ("The tray icon does not follow the theme")
