## MODIFIED Requirements

### Requirement: Tray icon independent of the theme
The system SHALL NOT change the tray icon with the in-app theme (Moth or Vesper); the tray icon SHALL follow the operating system theme instead.

#### Scenario: Vesper with the tray icon
- **WHEN** the Vesper theme is active in the app and the OS theme is dark
- **THEN** the tray icon uses the dark-OS variant, identical to the one used with Moth
