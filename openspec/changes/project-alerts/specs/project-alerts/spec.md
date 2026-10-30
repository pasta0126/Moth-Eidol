## Purpose

Defines how log entries become alert-level events per project, how repeated events are grouped, and how the user reviews them as pending or seen.

## ADDED Requirements

### Requirement: Level-based alert production
The system SHALL treat as alert-level every entry of an enabled project whose level is error or above, SHALL treat warning entries as alert-level only when the project has warning alerts enabled, and SHALL NOT treat information level or below, or unknown level, as alert-level.

#### Scenario: Error entry
- **WHEN** an entry of level error is emitted by an enabled project
- **THEN** an alert event is produced with the project, source, entry text, level and time

#### Scenario: Warning with warnings disabled
- **WHEN** a warning entry is emitted by a project with warning alerts disabled
- **THEN** no alert event is produced and the entry is still shown in the tail

#### Scenario: Warning with warnings enabled
- **WHEN** a warning entry is emitted by a project with warning alerts enabled
- **THEN** an alert event is produced

#### Scenario: Information entry
- **WHEN** an information entry is emitted
- **THEN** no alert event is produced

#### Scenario: Disabled project
- **WHEN** an entry is emitted by a disabled project
- **THEN** no alert event is produced

### Requirement: Multi-line alert content
The system SHALL keep the full entry, including its continuation lines, in each occurrence, within a bounded length.

#### Scenario: Error with stack trace
- **WHEN** an error entry with a stack trace is recorded
- **THEN** the occurrence contains the header and the stack trace text

### Requirement: Event groups
The system SHALL group the alert-level entries of a project that have the same level, the same normalized message and, when they contain a stack trace, the same exception type, keeping the number of occurrences and the time of the first and the last one.

#### Scenario: Repeated error with different ids
- **WHEN** the same error message is logged twice differing only in numeric ids
- **THEN** both occurrences belong to one group whose count is two

#### Scenario: Different errors
- **WHEN** two alert-level entries have different normalized messages
- **THEN** they belong to different groups

#### Scenario: Same message, different exception
- **WHEN** two entries have the same message but their stack traces start with different exception types
- **THEN** they belong to different groups

#### Scenario: Bounded detail
- **WHEN** a group has more occurrences than the stored limit
- **THEN** the count keeps increasing and only the most recent occurrences keep their full text

### Requirement: Pending and seen
The system SHALL consider a group pending for its occurrences since the user last marked it as seen, SHALL allow marking one group or all groups of a project as seen, and SHALL make a group pending again when a new occurrence arrives.

#### Scenario: Mark as seen
- **WHEN** the user marks a pending group as seen
- **THEN** the group has no pending occurrences

#### Scenario: Error returns
- **WHEN** a new occurrence arrives for a group that was marked as seen
- **THEN** the group becomes pending with only the new occurrences counted as pending

#### Scenario: Mark everything as seen
- **WHEN** the user marks all of a project as seen
- **THEN** none of its groups is pending

### Requirement: Events section
The system SHALL show in the project screen, as its landing section, the project's event groups with pending ones first and the most recent first, each with level, message, count, pending count and last time, filterable between pending and all.

#### Scenario: Open a project with pending errors
- **WHEN** the user opens a project that has pending groups
- **THEN** the events section is shown with the pending groups at the top

#### Scenario: Group detail
- **WHEN** the user opens a group
- **THEN** its recent occurrences are shown with their time, source and full text, and the group can be marked as seen from there

### Requirement: Tile status from pending events
The system SHALL show on each dashboard tile the number of pending occurrences and a status by the worst pending level, and SHALL show the clear status when nothing is pending.

#### Scenario: Pending errors
- **WHEN** a project has pending error groups
- **THEN** its tile shows the error status and the pending count

#### Scenario: Only warnings pending
- **WHEN** a project has only pending warning groups
- **THEN** its tile shows the warning status

#### Scenario: Nothing pending
- **WHEN** a project has no pending groups
- **THEN** its tile shows the clear status

### Requirement: Persistence and retention
The system SHALL persist groups, occurrences and seen state across runs and SHALL remove groups that were seen and have been quiet for longer than the configured retention, never removing pending groups automatically.

#### Scenario: Restart
- **WHEN** the application restarts
- **THEN** groups and their pending/seen state are restored

#### Scenario: Retention
- **WHEN** a seen group has had no occurrences for longer than the retention
- **THEN** it is removed
