## MODIFIED Requirements

### Requirement: Orphan data cleanup
The system SHALL remove at startup any stored source, read position, event group, occurrence or hourly count that belongs to a project or group that no longer exists, without affecting existing projects. When the sweep removed anything, the system SHALL then return the freed space to the disk in the background, without delaying the start of monitoring.

#### Scenario: Interrupted removal
- **WHEN** the application starts and the database holds events of a project that no longer exists
- **THEN** those events are removed and the other projects keep theirs

#### Scenario: Space returned after the sweep
- **WHEN** the startup sweep removes a significant amount of data
- **THEN** the database file becomes smaller shortly after startup while monitoring is already running

#### Scenario: Nothing to sweep
- **WHEN** the startup sweep removes nothing
- **THEN** no compaction is run
