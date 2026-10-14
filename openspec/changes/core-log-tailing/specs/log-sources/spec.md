## Purpose

Defines how the user configures and manages the log files being monitored and how their status is reported.

## ADDED Requirements

### Requirement: Adding and removing sources
The system SHALL allow adding and removing log sources by the path of a file, either local or on a network share accessible with the operating system user.

#### Scenario: Add a valid source
- **WHEN** the user adds the path of an existing, readable file
- **THEN** the source appears in the list, is active and starts being monitored

#### Scenario: Nonexistent path
- **WHEN** the user adds a path that does not exist
- **THEN** the source is saved with status "unavailable" and is retried periodically

#### Scenario: Remove a source
- **WHEN** the user removes a source
- **THEN** it stops being monitored and disappears from the list

### Requirement: Source activation
The system SHALL allow enabling or disabling a source without removing it.

#### Scenario: Disable
- **WHEN** the user disables a source
- **THEN** its reading stops and its configuration is kept

### Requirement: Visible source status
The system SHALL show each source's status: active, paused, unavailable or read error.

#### Scenario: Network resource down
- **WHEN** a source's network folder stops being accessible
- **THEN** its status becomes "unavailable" and returns to "active" automatically when it recovers

### Requirement: Source persistence
The system SHALL keep configured sources between runs.

#### Scenario: Restart
- **WHEN** the application restarts
- **THEN** the sources and their enabled state are restored
