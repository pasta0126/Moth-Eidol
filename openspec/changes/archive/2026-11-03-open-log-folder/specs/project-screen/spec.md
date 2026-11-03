## ADDED Requirements

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
