# project-screen Specification

## Purpose
Defines the per-project screen, where the user follows the project's live log and edits its configuration.

## Requirements

### Requirement: Project screen sections
The system SHALL show for the opened project an events section, a live tail section and a settings section, SHALL open on the events section, and SHALL allow switching between them without losing the tail.

#### Scenario: Open a project
- **WHEN** the user opens a project from its tile
- **THEN** the events section is shown first

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

### Requirement: Open log folder
The system SHALL provide in the project tail and in each source of the project settings an action that opens the source's location in the operating system file manager, revealing the followed file when the platform supports it and otherwise opening its folder.

#### Scenario: Open from the tail with one source selected
- **WHEN** the user selects a source and chooses "Open folder"
- **THEN** the file manager opens showing that source's current file or folder

#### Scenario: All sources in one folder
- **WHEN** "All sources" is selected and every source lives in the same folder
- **THEN** that folder is opened

#### Scenario: All sources in several folders
- **WHEN** "All sources" is selected and the sources live in different folders
- **THEN** the user is offered the list of folders to choose from

#### Scenario: Open from settings
- **WHEN** the user chooses the open action on a source in the project settings
- **THEN** the file manager opens that source's location

#### Scenario: Folder unavailable
- **WHEN** the folder does not exist or a network share is unreachable
- **THEN** a message states that the location is unavailable, shows its path, and the application keeps working

#### Scenario: No file manager available
- **WHEN** the platform has no way to open a file manager
- **THEN** a message shows the path so it can be copied

#### Scenario: Unresponsive share
- **WHEN** checking a network location takes longer than a few seconds
- **THEN** the interface stays responsive and reports the location as unavailable
