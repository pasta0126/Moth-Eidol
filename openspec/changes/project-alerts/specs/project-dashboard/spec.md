## MODIFIED Requirements

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
