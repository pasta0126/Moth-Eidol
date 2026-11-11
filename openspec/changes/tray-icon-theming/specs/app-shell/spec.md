## ADDED Requirements

### Requirement: Tray icon follows the OS theme
The system SHALL show a monochrome tray icon whose glyph contrasts with the operating system taskbar or menu bar (light glyph on dark, dark glyph on light), SHALL use a template image on macOS so the system tints it, and SHALL switch variant while running when the OS theme changes.

#### Scenario: Light OS theme
- **WHEN** the OS uses a light taskbar or menu bar
- **THEN** the tray icon is shown with a dark glyph

#### Scenario: Dark OS theme
- **WHEN** the OS uses a dark taskbar or menu bar
- **THEN** the tray icon is shown with a light glyph

#### Scenario: OS theme changes while running
- **WHEN** the user switches the OS theme while the app is running
- **THEN** the tray icon changes variant without restarting the app

#### Scenario: Alert state in both variants
- **WHEN** there are unread alerts
- **THEN** the alert state and the unread counter are visible and legible in both the light and dark variants
