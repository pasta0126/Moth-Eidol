## ADDED Requirements

### Requirement: Project retention
The system SHALL keep for each project a retention in days, from 1 to 365 and 10 by default, that the user can change in the project's storage section and that is kept between runs. While the application runs, the system SHALL remove automatically, at startup and at least once per hour, the project's history older than its retention with the same rules as "Keep only the last days": occurrences and hourly counts older than the cutoff are removed, and groups with no occurrence since the cutoff are removed together with their pending state; groups with activity since the cutoff keep their totals, first-seen date and pending/seen state. The freed space SHALL be returned to the disk progressively, without blocking monitoring. The automatic removal SHALL not send notifications; the tile, the pending counters and the events section SHALL reflect it.

#### Scenario: Default retention
- **WHEN** a project is created, or an existing project is opened for the first time after this version
- **THEN** its retention is 10 days

#### Scenario: Change the retention
- **WHEN** the user sets the retention of a project to 3 days in its storage section
- **THEN** the value is saved, is kept after a restart, and history older than 3 days is removed at once

#### Scenario: Old pending group
- **WHEN** a pending group of a project with a 10-day retention was last seen 12 days ago and the hourly cleanup runs
- **THEN** the group is removed, the project's pending count no longer includes it and no notification is sent

#### Scenario: Recent group with old occurrences
- **WHEN** a group with activity today also has occurrences older than the retention
- **THEN** those occurrences and its hourly counts older than the cutoff are removed, and the group is kept with its total count and pending/seen state

#### Scenario: Independent per project
- **WHEN** one project has a 3-day retention and another a 30-day retention
- **THEN** each one's history is trimmed to its own retention

#### Scenario: Invalid retention
- **WHEN** the user enters a retention outside 1–365 or not a whole number
- **THEN** it is not saved and the valid range is shown

#### Scenario: Application not running
- **WHEN** the application has been closed for several days and starts again
- **THEN** history older than each project's retention is removed shortly after startup
