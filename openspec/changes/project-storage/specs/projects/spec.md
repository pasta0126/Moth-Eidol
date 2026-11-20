## MODIFIED Requirements

### Requirement: Project management
The system SHALL allow creating, renaming, enabling, disabling and removing projects, each with a unique non-empty name. Removing a project SHALL delete all its stored data in a single operation: the project, its sources, their read positions, and its event groups, occurrences and hourly counts; either everything is removed or nothing is.

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

#### Scenario: Nothing left behind
- **WHEN** a project with sources and recorded events is removed
- **THEN** the database holds no source, read position, event group, occurrence or hourly count of that project, and its freed space is returned to the disk

#### Scenario: Same name again
- **WHEN** a removed project is created again with the same name and sources
- **THEN** it starts with no events and its sources are read as new
