## MODIFIED Requirements

### Requirement: Create from the dashboard
The system SHALL let the user create a project from the dashboard through the project setup wizard (see `project-wizard`), and SHALL then open the new project's screen on its events section.

#### Scenario: New project
- **WHEN** the user completes the wizard for a project named "Billing"
- **THEN** it appears on the dashboard and its screen opens on the events section

#### Scenario: Wizard cancelled
- **WHEN** the user cancels the wizard
- **THEN** the dashboard is shown unchanged
