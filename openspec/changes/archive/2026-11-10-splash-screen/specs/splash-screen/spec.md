## Purpose
Defines the branded splash window shown while the application starts and when it appears and disappears.

## ADDED Requirements

### Requirement: Splash on launch
The system SHALL show a borderless splash window with the application artwork when the user launches the application.

#### Scenario: Manual launch
- **WHEN** the user launches the application
- **THEN** the splash appears immediately, before the main window

### Requirement: Splash dismissal
The system SHALL close the splash when startup finishes and the main window is ready, but not before it has been visible for at least 2 seconds, and SHALL also close it if startup fails.

#### Scenario: Startup completes
- **WHEN** the main window is ready
- **THEN** the splash closes and the main window is shown

#### Scenario: Very fast startup
- **WHEN** the main window is ready after less than 2 seconds
- **THEN** the splash stays until it has been visible for 2 seconds

#### Scenario: Startup fails
- **WHEN** startup raises an error
- **THEN** the splash closes and the error is handled as it is today

### Requirement: No version on the splash
The splash SHALL NOT display the application version.

#### Scenario: Splash content
- **WHEN** the splash is visible
- **THEN** it shows the artwork with the application name and tagline, never the version, and with the dark colors whatever theme is active

### Requirement: Second instance
The system SHALL NOT show a splash when a second instance is launched and hands over to the running one.

#### Scenario: Launch while already running
- **WHEN** the user launches the application while it is already running
- **THEN** no splash is shown and the running instance is brought to the front

### Requirement: Splash can be disabled
The system SHALL offer a setting to turn the splash off, on by default, and SHALL NOT show the splash on the next launches while it is off.

#### Scenario: Splash off
- **WHEN** the user turns off "Show the splash screen at startup" and launches the application again
- **THEN** no splash is shown and the main window appears as soon as it is ready

#### Scenario: Default
- **WHEN** the application runs for the first time
- **THEN** the splash is shown
