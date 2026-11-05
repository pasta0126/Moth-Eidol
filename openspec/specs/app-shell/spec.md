# app-shell Specification

## Purpose
Defines the desktop application shell: main window, system tray icon, background execution and visible version.

## Requirements

### Requirement: Application startup
The system SHALL start a main window on Windows, macOS and Linux and SHALL show the application's semantic version `x.y.z` in the interface.

#### Scenario: Normal startup
- **WHEN** the user runs the application
- **THEN** the main window is shown with the current version visible

### Requirement: Version always visible
The system SHALL show the running version in a persistent footer of the main window that does not depend on the current content, SHALL separate the footer from the content above it with a small space so it never touches it, and SHALL NOT show the version in the main window title or in the tray icon tooltip, which carry only the application name.

#### Scenario: Identify the running version
- **WHEN** the user looks at the footer of the main window
- **THEN** the current semantic version `x.y.z` is shown

#### Scenario: Title and tooltip without version
- **WHEN** the user looks at the main window title or hovers the tray icon
- **THEN** only the application name is shown, without any version

#### Scenario: Footer separated from the content
- **WHEN** any screen is shown
- **THEN** there is a visible gap between the content and the footer and the footer text does not touch the content above

### Requirement: Single instance
The system SHALL allow only one instance per user session.

#### Scenario: Second launch
- **WHEN** the application is already running and the user launches it again
- **THEN** no second process is created and the existing instance's window is restored

### Requirement: System tray icon
The system SHALL show a tray icon while the application is running, with a menu containing at least the actions "Open" and "Quit".

#### Scenario: Open from the tray
- **WHEN** the user selects "Open" in the icon menu
- **THEN** the main window is shown and takes focus

#### Scenario: Quit from the tray
- **WHEN** the user selects "Quit"
- **THEN** the application terminates completely, including the tray icon

### Requirement: Background execution
The system SHALL keep the process alive when the main window is closed, hiding it to the tray, and SHALL terminate only through the explicit "Quit" action.

#### Scenario: Window close
- **WHEN** the user closes the main window
- **THEN** the window is hidden and the process and tray icon remain active

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
