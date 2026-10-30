## Purpose

Defines how log entries become alert events according to their level and their project's configuration, and how alerts are kept in a history.

## ADDED Requirements

### Requirement: Level-based alert production
The system SHALL produce an alert event for each entry of an enabled project whose level is error or above, SHALL produce one for warning entries only when the project has warning alerts enabled, and SHALL NOT produce one for information level or below or for unknown level.

#### Scenario: Error entry
- **WHEN** an entry of level error is emitted by an enabled project
- **THEN** an alert event is produced with the project, source, entry text, level and time

#### Scenario: Warning with warnings disabled
- **WHEN** a warning entry is emitted by a project with warning alerts disabled
- **THEN** no alert event is produced and the entry is still shown in the viewer

#### Scenario: Warning with warnings enabled
- **WHEN** a warning entry is emitted by a project with warning alerts enabled
- **THEN** an alert event is produced

#### Scenario: Information entry
- **WHEN** an information entry is emitted
- **THEN** no alert event is produced

#### Scenario: Disabled project
- **WHEN** an entry is emitted by a disabled project
- **THEN** no alert event is produced

### Requirement: Multi-line alert content
The system SHALL include the full entry, including its continuation lines, in the alert event, within the stored length limit.

#### Scenario: Error with stack trace
- **WHEN** an error entry with a stack trace produces an alert
- **THEN** the alert event contains the header and the stack trace text

### Requirement: Persistence of the alert history
The system SHALL persist an alert history across runs and SHALL apply a configurable retention to it.

#### Scenario: Restart
- **WHEN** the application restarts
- **THEN** the alert history is restored

#### Scenario: Retention
- **WHEN** history entries exceed the configured retention
- **THEN** the oldest entries are removed

### Requirement: History view
The system SHALL show the alert history, newest first, with project, source, level, text and time, filterable by project and level.

#### Scenario: Browse history
- **WHEN** the user opens the history view
- **THEN** past alerts are listed newest first

#### Scenario: Filter by project
- **WHEN** the user filters by a project
- **THEN** only that project's alerts are listed
