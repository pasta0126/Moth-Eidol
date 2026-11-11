## ADDED Requirements

### Requirement: Export project configuration
The system SHALL export a project's configuration to a UTF-8 JSON file chosen by the user, containing a format version and the project's name, accent color, sources, alert level settings, frequency settings and, when supported, keywords and rules, and SHALL NOT include events, offsets, pending/seen state or application settings.

#### Scenario: Export a project
- **WHEN** the user exports a project from its settings
- **THEN** a JSON file with the project's configuration is written where the user chose, with no event data

### Requirement: Import project configuration
The system SHALL import a configuration file by showing a preview of the project it describes and creating a new project only after the user confirms, SHALL create it atomically, and SHALL NOT overwrite or modify an existing project.

#### Scenario: Import a valid file
- **WHEN** the user selects a valid file and confirms the preview
- **THEN** a new project with that configuration appears on the dashboard

#### Scenario: Cancel the preview
- **WHEN** the user cancels at the preview
- **THEN** nothing is created

#### Scenario: Name already in use
- **WHEN** the imported name equals an existing project's name
- **THEN** the preview proposes a unique name (for example with a numeric suffix) that the user can edit

### Requirement: Validation of imported files
The system SHALL validate the file before showing the preview, SHALL reject files that are not valid JSON, that have an unknown or newer format version, or that fail the same validation rules as manual configuration, and SHALL explain the reason without creating anything.

#### Scenario: Corrupt file
- **WHEN** the user selects a file that is not valid JSON
- **THEN** an error is shown and nothing is created

#### Scenario: Newer format version
- **WHEN** the file declares a format version newer than the app supports
- **THEN** the import is rejected with a message asking to update the app

### Requirement: Machine-specific sources
The system SHALL warn, in the preview, about imported sources whose folder or file does not exist or is not readable on this machine, and SHALL let the user edit the paths before confirming or import them as they are.

#### Scenario: Folder missing on this PC
- **WHEN** an imported source points to a folder that does not exist here
- **THEN** the preview flags that source and lets the user change the path

### Requirement: Forward compatibility
The system SHALL ignore unknown fields in a file of a supported format version and SHALL import the fields it understands.

#### Scenario: Extra fields
- **WHEN** the file contains fields unknown to this version
- **THEN** they are ignored and the project is imported
