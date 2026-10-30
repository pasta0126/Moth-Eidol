## Purpose

Defines the per-project screen, where the user follows the project's live log and edits its configuration.

## ADDED Requirements

### Requirement: Project screen sections
The system SHALL show for the opened project a live tail section and a settings section, and SHALL allow switching between them without losing the tail.

#### Scenario: Switch sections
- **WHEN** the user moves from the tail to the settings and back
- **THEN** the tail still shows the lines that arrived in the meantime

### Requirement: Project tail
The system SHALL show in the tail the new lines of all the project's sources, SHALL allow limiting the view to one source, and SHALL keep the auto-scroll, pause and line-limit behavior of the live viewer.

#### Scenario: All sources
- **WHEN** the project has two active sources
- **THEN** lines from both appear in the tail

#### Scenario: One source
- **WHEN** the user selects one source in the tail
- **THEN** only that source's lines are shown

### Requirement: Project settings
The system SHALL let the user edit in the project's screen the name, enabled state, sources, warning threshold and frequency mode, and SHALL allow removing the project.

#### Scenario: Edit parameters
- **WHEN** the user changes the warning threshold in the settings
- **THEN** the change is saved and applies without restarting

#### Scenario: Remove the project
- **WHEN** the user removes the project after confirming
- **THEN** the dashboard is shown without that project

### Requirement: Persistent version
The system SHALL keep the application version visible on the project screen.

#### Scenario: Version footer
- **WHEN** any project section is shown
- **THEN** the version footer is visible
