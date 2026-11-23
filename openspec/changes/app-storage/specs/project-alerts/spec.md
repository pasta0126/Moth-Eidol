## MODIFIED Requirements

### Requirement: Persistence and retention
The system SHALL persist groups, occurrences and seen state across runs and SHALL apply each project's retention automatically, as defined by the project storage retention: history older than the project's retention days is removed, pending groups included.

#### Scenario: Restart
- **WHEN** the application restarts
- **THEN** groups and their pending/seen state are restored

#### Scenario: Retention
- **WHEN** a group of a project has had no occurrences for longer than the project's retention, seen or pending
- **THEN** it is removed
