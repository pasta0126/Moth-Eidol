## ADDED Requirements

### Requirement: Sources belong to a project
The system SHALL associate every log source with exactly one project.

#### Scenario: Add a source to a project
- **WHEN** the user adds a source while editing a project
- **THEN** the source belongs to that project and follows its enabled state

#### Scenario: Remove the project
- **WHEN** a project is removed
- **THEN** its sources are removed with it
