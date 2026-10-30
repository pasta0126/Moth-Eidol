## Purpose

Defines monitored projects: a named group of log sources with its own alert configuration.

## ADDED Requirements

### Requirement: Project management
The system SHALL allow creating, editing, enabling, disabling and removing projects, each with a unique name and one or more log sources.

#### Scenario: Create a project
- **WHEN** the user creates a project with a name and a folder with a file pattern
- **THEN** the project is saved, enabled, and its sources start being monitored

#### Scenario: Duplicate name
- **WHEN** the user saves a project whose name already exists
- **THEN** the project is rejected and the error is shown

#### Scenario: Disable a project
- **WHEN** the user disables a project
- **THEN** all its sources stop being monitored, its alerts stop and its configuration is kept

#### Scenario: Remove a project
- **WHEN** the user removes a project
- **THEN** its sources stop being monitored and the project disappears from the list

### Requirement: Alert level configuration
The system SHALL let each project choose whether warnings raise alerts, SHALL always raise alerts for error level and above, and SHALL never raise alerts for information level or below.

#### Scenario: Warnings off
- **WHEN** a project has warning alerts disabled and a warning entry is emitted
- **THEN** the entry is shown but no alert is raised

#### Scenario: Warnings on
- **WHEN** a project has warning alerts enabled and a warning entry is emitted
- **THEN** an alert is raised for that entry

#### Scenario: Error always alerts
- **WHEN** an entry of level error or fatal is emitted by any enabled project
- **THEN** an alert is raised regardless of the project's warning setting

#### Scenario: Information never alerts
- **WHEN** an information entry is emitted
- **THEN** it is shown in the viewer and no alert is raised

### Requirement: Alert frequency configuration
The system SHALL let each project choose between immediate alerts with a cooldown and a periodic summary with an interval, with defaults applied to new projects.

#### Scenario: Immediate with cooldown
- **WHEN** a project uses immediate mode with a cooldown
- **THEN** the first alert is raised at once and further ones are grouped until the cooldown elapses

#### Scenario: Periodic summary
- **WHEN** a project uses summary mode with an interval
- **THEN** one aggregated alert is raised per interval if there were alerting entries

### Requirement: Project persistence and hot reload
The system SHALL persist projects between runs and SHALL apply changes without restarting the application.

#### Scenario: Restart
- **WHEN** the application restarts
- **THEN** projects, their sources and alert configuration are restored

#### Scenario: Edit while running
- **WHEN** the user changes a project's configuration while it is monitored
- **THEN** the change applies to subsequent entries without restarting

### Requirement: Project status
The system SHALL show each project's status: sources available or unavailable, time of the last entry and recent counts of warnings and errors.

#### Scenario: Source unavailable
- **WHEN** a project's source becomes unavailable
- **THEN** the project is shown as degraded with the affected source identified
