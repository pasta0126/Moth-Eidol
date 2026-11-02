## MODIFIED Requirements

### Requirement: Version always visible
The system SHALL show the running version in a persistent footer of the main window that does not depend on the current content, and SHALL NOT show it in the main window title or in the tray icon tooltip, which carry only the application name.

#### Scenario: Identify the running version
- **WHEN** the user looks at the footer of the main window
- **THEN** the current semantic version `x.y.z` is shown

#### Scenario: Title and tooltip without version
- **WHEN** the user looks at the main window title or hovers the tray icon
- **THEN** only the application name is shown, without any version
