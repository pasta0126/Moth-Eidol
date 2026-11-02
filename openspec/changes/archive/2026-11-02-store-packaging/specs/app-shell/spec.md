## MODIFIED Requirements

### Requirement: Start with session
The system SHALL offer a configurable option to start with the user's session, disabled by default, and SHALL make it work both when running unpackaged and when running as an MSIX package, where the user may also manage it from the OS startup apps settings.

#### Scenario: Enable start with session
- **WHEN** the user enables the option
- **THEN** the application starts automatically when the user logs in to the operating system

#### Scenario: Enable start with session when packaged
- **WHEN** the packaged application enables the option
- **THEN** the OS registers it as a startup app, and the option reflects the state if the user later disables it from the OS settings

#### Scenario: Blocked by the OS
- **WHEN** the OS or policy prevents enabling the startup task
- **THEN** the option is shown as unavailable with the reason

#### Scenario: Unsupported platform
- **WHEN** the platform does not support the option yet
- **THEN** the option is shown disabled with an explanation
