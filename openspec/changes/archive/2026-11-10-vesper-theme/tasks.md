## 1. Definition

- [x] 1.1 Settle the open questions in the design (primary color, semantic colors, tray and toast theming, artwork variants) and update the specs accordingly
- [x] 1.2 Show a visual preview of the Vesper palette on real screens and get the maintainer's approval before building it

## 2. Theme resources

- [x] 2.1 Add the Vesper theme dictionary with the same `Moth*Brush` names and legible semantic colors
- [x] 2.2 Audit the views for hard-coded colors and replace them with brush resources
- [x] 2.3 Readable accent text: shift the accent used as text until it reaches 4.5:1 against the tile background (Core helper with tests); keep the tray icon on the Moth colors

## 3. Selection and persistence

- [x] 3.1 Add the theme setting to the stored application settings (default Moth)
- [x] 3.2 Add the theme selector to the application settings and apply the choice at runtime and at startup

## 4. Verification

- [x] 4.1 Unit tests for the setting and its default
- [x] 4.2 Build and run the tests
- [x] 4.3 Check manually every screen, the toast and the footer in both themes (deferred to the maintainer)
- [x] 4.4 Update the UI theme section of `AGENTS.md` and add the entry to `CHANGELOG.md`
