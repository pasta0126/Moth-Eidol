# project-storage Specification

## Purpose
Lets the user see how much a project weighs in the local database and lighten it, so the event history stays bounded and never harms the application or the computer.

## Requirements

### Requirement: Project storage usage
The system SHALL show for the opened project its load in the local database: the number of event groups, the number of retained occurrences, the number of hourly counts, an approximate size of the project's data, the dates of its oldest and newest occurrence, and the size of the whole database file. The figures SHALL be computed when the section is opened and when the user refreshes it, never on every new entry.

#### Scenario: Project with events
- **WHEN** the user opens the storage section of a project that has recorded events
- **THEN** the groups, occurrences, hourly counts, approximate size, oldest and newest occurrence and the database file size are shown

#### Scenario: Project without events
- **WHEN** the user opens the storage section of a project with no recorded events
- **THEN** the counts are zero, no dates are shown and the cleanup actions are disabled

#### Scenario: Refresh
- **WHEN** new events are recorded while the section is open and the user chooses refresh
- **THEN** the figures include the new events

### Requirement: Clear all project events
The system SHALL allow the user, after confirming, to remove every recorded event of the project, including pending ones: its groups, occurrences and hourly counts. The project, its sources, its settings and its read positions SHALL be kept, so lines already read are not alerted again.

#### Scenario: Clear all
- **WHEN** the user chooses "Clear all" and confirms
- **THEN** the project has no event groups, its events section is empty, its tile shows no pending events and its sources keep being followed from where they were

#### Scenario: Cancel
- **WHEN** the user chooses "Clear all" and cancels the confirmation
- **THEN** nothing is removed

#### Scenario: Other projects untouched
- **WHEN** the user clears all the events of one project
- **THEN** the events of the other projects are unchanged

### Requirement: Keep only the last days
The system SHALL allow the user, after confirming, to remove the project's history older than a number of days they choose (from 1 to 365, default 7): occurrences and hourly counts older than the cutoff are removed, and groups with no occurrence since the cutoff are removed together with their pending state. Groups with activity since the cutoff SHALL be kept with their totals, first-seen date and pending/seen state unchanged.

#### Scenario: Old and recent groups
- **WHEN** a project has a group last seen 20 days ago and another seen today, and the user keeps the last 7 days and confirms
- **THEN** the first group is removed and the second is kept

#### Scenario: Old occurrences of a recent group
- **WHEN** a kept group has occurrences older than the cutoff
- **THEN** those occurrences and its hourly counts older than the cutoff are removed, and its total count is unchanged

#### Scenario: Invalid number of days
- **WHEN** the user enters a number of days outside 1–365 or not a number
- **THEN** the action is not available and the valid range is shown

### Requirement: Cleanup result and disk space
The system SHALL report after a cleanup how many groups and occurrences were removed, SHALL return the freed space to the disk so the database file shrinks, SHALL refresh the project's events section, tile and pending counters, and SHALL keep the application responsive while the cleanup runs.

#### Scenario: File shrinks
- **WHEN** a cleanup removes a significant amount of data
- **THEN** the database file size shown afterwards is smaller than before

#### Scenario: Result shown
- **WHEN** a cleanup finishes
- **THEN** the number of removed groups and occurrences is shown and the figures are refreshed

#### Scenario: Monitoring continues
- **WHEN** a cleanup is running
- **THEN** the tail keeps showing new lines and new events are recorded once it finishes

### Requirement: Storage size warning
The system SHALL keep for each project a storage threshold in megabytes, from 1 to 10240 and 100 by default, that the user can change in the project's storage section and that is kept between runs. When the project's approximate size is over its threshold, the system SHALL show it on the project's tile and in its storage section, next to the cleanup actions, without removing anything automatically and without sending notifications. The size SHALL be measured at startup, at least once per hour while the application runs, after every cleanup and when the storage section is opened or refreshed.

#### Scenario: Default threshold
- **WHEN** a project is created, or an existing project is opened for the first time after this version
- **THEN** its storage threshold is 100 MB

#### Scenario: Change the threshold
- **WHEN** the user sets the threshold of a project to 20 MB in its storage section
- **THEN** the value is saved, is kept after a restart, and the warning is evaluated again against it at once

#### Scenario: Over the threshold
- **WHEN** a project's approximate size is over its threshold
- **THEN** its tile shows a line stating that its storage is over the threshold and its storage section shows the size and the threshold highlighted next to the cleanup actions

#### Scenario: Back under the threshold
- **WHEN** a cleanup leaves the project's approximate size under its threshold
- **THEN** the warning disappears from its tile and from its storage section

#### Scenario: Only a warning
- **WHEN** a project goes over its threshold
- **THEN** no event is removed, no notification is sent and the status dot of its tile is unchanged

#### Scenario: Invalid threshold
- **WHEN** the user enters a threshold outside 1–10240 or not a whole number
- **THEN** it is not saved and the valid range is shown

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
