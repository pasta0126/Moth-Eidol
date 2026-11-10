# projects Specification

## Purpose
Defines monitored projects: a named group of log sources with its own alert configuration.

## Requirements

### Requirement: Project management
The system SHALL allow creating, renaming, enabling, disabling and removing projects, each with a unique non-empty name.

#### Scenario: Create a project with a name only
- **WHEN** the user creates a project giving only a name
- **THEN** the project is saved and enabled with no sources, and is shown as not configured

#### Scenario: Duplicate name
- **WHEN** the user saves a project whose name already exists
- **THEN** the project is rejected and the error is shown

#### Scenario: Disable a project
- **WHEN** the user disables a project
- **THEN** all its sources stop being monitored, its alerts stop and its configuration is kept

#### Scenario: Remove a project
- **WHEN** the user removes a project
- **THEN** its sources stop being monitored and the project disappears from the dashboard

### Requirement: Project sources
The system SHALL let the user add, enable, disable and remove the log sources of a project, as a file or as a folder with a file-name pattern.

#### Scenario: Add a folder source to a project
- **WHEN** the user adds a folder and a pattern to a project
- **THEN** the project starts monitoring the newest matching file

### Requirement: Alert level configuration
The system SHALL let each project choose whether warnings raise alerts, SHALL always raise alerts for error level and above, and SHALL never raise alerts for information level or below.

#### Scenario: Warnings off
- **WHEN** a project has warning alerts disabled
- **THEN** warning entries are shown in its tail but are not highlighted as alerts

#### Scenario: Warnings on
- **WHEN** a project has warning alerts enabled
- **THEN** warning entries are treated as alert-level entries

#### Scenario: Error always alerts
- **WHEN** an entry of level error or fatal is emitted by an enabled project
- **THEN** it is treated as an alert-level entry regardless of the warning setting

#### Scenario: Information never alerts
- **WHEN** an information entry is emitted
- **THEN** it is shown in the tail and is never treated as an alert-level entry

### Requirement: Alert frequency configuration
The system SHALL let each project choose between immediate alerts with a cooldown and a periodic summary with an interval, with defaults applied to new projects.

#### Scenario: Defaults
- **WHEN** a project is created
- **THEN** warnings are off and the frequency mode is immediate with the default cooldown

### Requirement: Persistence and hot reload
The system SHALL persist projects between runs and SHALL apply changes without restarting the application.

#### Scenario: Restart
- **WHEN** the application restarts
- **THEN** projects, their sources and alert configuration are restored

#### Scenario: Edit while running
- **WHEN** the user changes a project's configuration while it is monitored
- **THEN** the change applies to subsequent entries without restarting

### Requirement: Project accent color
The system SHALL let the user choose, in the project's settings, an accent color from a palette of 16 colors or by typing a hex value (`#RRGGBB`, `RRGGBB` or `#RGB`), and SHALL offer a way to return to the default color. An invalid hex value SHALL be rejected with a clear message and not saved.

#### Scenario: Pick from the palette
- **WHEN** the user selects one of the 16 swatches and saves
- **THEN** the project's accent is that color and its tile reflects it

#### Scenario: Type a hex value
- **WHEN** the user types `3fa7d6` in the hex field
- **THEN** it is normalized to `#3FA7D6`, shown as selected, and saved

#### Scenario: Invalid hex
- **WHEN** the user types `#12ZZ99`
- **THEN** an error is shown next to the field and the previous color is kept

#### Scenario: Back to default
- **WHEN** the user chooses the default color
- **THEN** the project has no accent stored and uses the default accent

### Requirement: Project order
The system SHALL store a position for each project; new projects SHALL be placed last, and deleting a project SHALL NOT disturb the relative order of the others.

#### Scenario: New project
- **WHEN** a project is created
- **THEN** it appears after all existing projects
