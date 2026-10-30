## ADDED Requirements

### Requirement: Sources belong to a project
The system SHALL associate every log source with exactly one project.

#### Scenario: Add a source to a project
- **WHEN** the user adds a source while editing a project
- **THEN** the source belongs to that project and follows its enabled state

#### Scenario: Remove the project
- **WHEN** a project is removed
- **THEN** its sources are removed with it

### Requirement: Migration of existing sources
The system SHALL keep sources created before projects existed, together with their read positions, by moving them into a project named "Unassigned".

#### Scenario: Upgrade with existing sources
- **WHEN** the application starts after an upgrade and sources exist without a project
- **THEN** they appear in the "Unassigned" project, keep monitoring and do not repeat lines already read
