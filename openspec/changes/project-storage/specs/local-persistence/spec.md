## ADDED Requirements

### Requirement: Orphan data cleanup
The system SHALL remove at startup any stored source, read position, event group, occurrence or hourly count that belongs to a project or group that no longer exists, without affecting existing projects.

#### Scenario: Interrupted removal
- **WHEN** the application starts and the database holds events of a project that no longer exists
- **THEN** those events are removed and the other projects keep theirs

### Requirement: Bounded database file
The system SHALL return to the disk the space freed by manual cleanups and project removals, and SHALL keep the database write-ahead log from growing without bound while the application runs.

#### Scenario: After a project removal
- **WHEN** a project holding a large share of the data is removed
- **THEN** the database file becomes smaller

#### Scenario: Long run
- **WHEN** the application runs for days recording events
- **THEN** the write-ahead log file is periodically truncated
