## Purpose

Defines the desktop application shell: main window, system tray icon, background execution and visible version.

## ADDED Requirements

### Requirement: Application startup
The system SHALL start a main window on Windows, macOS and Linux and SHALL show the application's semantic version `x.y.z` in the interface.

#### Scenario: Normal startup
- **WHEN** the user runs the application
- **THEN** the main window is shown with the current version visible

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
The system SHALL offer a configurable option to start with the user's session, disabled by default.

#### Scenario: Enable start with session
- **WHEN** the user enables the option
- **THEN** the application starts automatically when the user logs in to the operating system

#### Scenario: Unsupported platform
- **WHEN** the platform does not support the option yet
- **THEN** the option is shown disabled with an explanation
