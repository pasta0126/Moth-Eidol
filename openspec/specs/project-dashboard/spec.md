# project-dashboard Specification

## Purpose
Defines the home screen: a dashboard with one tile per project that shows at a glance whether anything needs attention.

## Requirements

### Requirement: Dashboard as home
The system SHALL open on a dashboard showing one tile per project, ordered by name, and a tile to create a new project.

#### Scenario: Application start
- **WHEN** the application opens
- **THEN** the dashboard is shown with a tile for every project

#### Scenario: No projects yet
- **WHEN** there are no projects
- **THEN** the dashboard shows only the new-project tile and a short hint

### Requirement: Tile status
The system SHALL show on each tile the project's name and a status distinguishable at a glance: healthy, degraded (a source is unavailable or has a read error), not configured (no sources) or disabled, and SHALL combine it with the pending state of its events (see `project-alerts`).

#### Scenario: Healthy project
- **WHEN** all sources of an enabled project are active
- **THEN** its tile shows the healthy status

#### Scenario: Source down
- **WHEN** a source of a project becomes unavailable
- **THEN** the project's tile shows the degraded status and names the affected source

#### Scenario: Project without sources
- **WHEN** a project has no sources
- **THEN** its tile shows the not configured status

#### Scenario: Pending events outweigh healthy
- **WHEN** a healthy project has pending event groups
- **THEN** its tile shows the pending state of its worst pending level

### Requirement: Create from the dashboard
The system SHALL let the user create a project from the dashboard through the project setup wizard (see `project-wizard`), and SHALL then open the new project's screen on its events section.

#### Scenario: New project
- **WHEN** the user completes the wizard for a project named "Billing"
- **THEN** it appears on the dashboard and its screen opens on the events section

#### Scenario: Wizard cancelled
- **WHEN** the user cancels the wizard
- **THEN** the dashboard is shown unchanged

### Requirement: Navigation
The system SHALL open a project's screen when its tile is clicked and SHALL allow returning to the dashboard from it, and SHALL keep application settings and the version visible or reachable from the dashboard.

#### Scenario: Open and return
- **WHEN** the user clicks a tile and then goes back
- **THEN** the project's screen opens and the dashboard is shown again
