## ADDED Requirements

### Requirement: Keywords in project configuration
The system SHALL include the project's keywords in its configuration, SHALL offer an optional keywords field in the setup wizard alerts step, and SHALL create them atomically with the project.

#### Scenario: Wizard with keywords
- **WHEN** the user enters keywords in the wizard and finishes
- **THEN** the project is created with those keywords

#### Scenario: Wizard cancelled
- **WHEN** the user cancels the wizard after entering keywords
- **THEN** nothing is created
