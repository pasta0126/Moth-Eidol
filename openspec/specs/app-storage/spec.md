# app-storage Specification

## Purpose
Gives the user one place in the application settings to see everything the local database holds across projects and to remove projects, their events and leftover data that no longer belongs to any project, so nothing is stored without the user being able to find it.

## Requirements

### Requirement: Application storage section
The system SHALL provide a Storage section in the application settings, reachable from the dashboard, showing the location of the data folder, the size of the database file including its write-ahead log, the space a compaction would return to the disk, and the total approximate size of all projects. The figures SHALL be computed when the section is opened and when the user refreshes it, never on every new entry, and computing them SHALL keep the application responsive.

#### Scenario: Open the section
- **WHEN** the user opens the Storage section of the application settings
- **THEN** the data folder, the database file size, the reclaimable space and the total approximate size of the projects are shown

#### Scenario: Refresh
- **WHEN** new events are recorded while the section is open and the user chooses refresh
- **THEN** the figures include the new events

### Requirement: Stored projects overview
The system SHALL list in the application Storage section every stored project with its name, its state (enabled, disabled, or not configured when it has no sources), its number of event groups and retained occurrences, its approximate size, its retention in days and the date of its newest occurrence, sorted by approximate size, largest first. Disabled and not-configured projects that hold events SHALL be marked as cleanup candidates. Each row SHALL allow opening the project's screen.

#### Scenario: Projects listed
- **WHEN** there are three projects, one enabled with events, one disabled with events and one without sources and without events
- **THEN** the three are listed with their state, figures and retention, and only the disabled one is marked as a cleanup candidate

#### Scenario: No projects
- **WHEN** there are no projects
- **THEN** the list states that there are no projects and the database summary is still shown

#### Scenario: Open a project
- **WHEN** the user chooses to open a project from the list
- **THEN** that project's screen is shown

### Requirement: Clear a project's events from the application storage
The system SHALL allow the user, after confirming, to remove every recorded event of a listed project with the same result as "Clear all" in the project's own Storage section: groups, occurrences and hourly counts are removed, pending ones included, while the project, its sources, its settings and its read positions are kept. The confirmation SHALL state the project and how many groups are pending. The action SHALL be disabled for a project with no events.

#### Scenario: Clear from the list
- **WHEN** the user clears the events of a project from the list and confirms
- **THEN** the project has no events, its tile shows no pending events, its sources keep being followed from where they were and its row shows zero groups and occurrences

#### Scenario: Cancel
- **WHEN** the user cancels the confirmation
- **THEN** nothing is removed

### Requirement: Remove a project from the application storage
The system SHALL allow the user, after confirming, to remove a listed project with the same result as removing it from its own settings: its monitoring stops and the project, its sources, their read positions and all its events are removed in a single operation. The confirmation SHALL state the project's name, its number of sources and its number of event groups, pending ones included.

#### Scenario: Remove from the list
- **WHEN** the user removes a project from the list and confirms
- **THEN** the project disappears from the list and from the dashboard, its sources stop being monitored and the database holds nothing of it

#### Scenario: Cancel
- **WHEN** the user cancels the confirmation
- **THEN** the project is kept unchanged

### Requirement: Leftover data
The system SHALL show in the application Storage section the data that belongs to no existing project or source (sources, read positions, event groups, occurrences and hourly counts) with their number of rows, and the backup copies of a corrupt database kept in the data folder with their names and sizes. The system SHALL allow the user, after confirming, to remove all of it; the current database file and the data of existing projects SHALL never be touched. When there are no leftovers, the section SHALL say so and the action SHALL be disabled.

#### Scenario: Orphan rows
- **WHEN** the database holds occurrences whose group no longer exists and the user removes the leftovers and confirms
- **THEN** those occurrences are removed and the events of the existing projects are unchanged

#### Scenario: Corrupt backups
- **WHEN** the data folder holds a backup copy of a corrupt database and the user removes the leftovers and confirms
- **THEN** the backup file is deleted and the current database is untouched

#### Scenario: Nothing left over
- **WHEN** there are no orphan rows and no backup copies
- **THEN** the section states that nothing is left over and the remove action is disabled

#### Scenario: Backup that cannot be deleted
- **WHEN** a backup copy cannot be deleted (for example, it is open in another program)
- **THEN** the other leftovers are removed, the failure is shown for that file and the file is listed again

### Requirement: Compact the database on demand
The system SHALL allow the user to return the database's free space to the disk from the application Storage section, SHALL keep the application responsive and monitoring running while it compacts, and SHALL show the file size before and after. The action SHALL be disabled when there is no reclaimable space.

#### Scenario: Compact
- **WHEN** the database has reclaimable space and the user chooses to compact it
- **THEN** the database file becomes smaller and the reclaimable space shown afterwards is close to zero

#### Scenario: Monitoring continues
- **WHEN** a compaction is running
- **THEN** the tail keeps showing new lines and new events are recorded once it finishes

### Requirement: Result of an application storage action
The system SHALL report after each action of the application Storage section what was removed (projects, groups, occurrences, rows, files) and the space freed, SHALL return the freed space to the disk after removing data, and SHALL refresh the section, the dashboard tiles, the pending counters and any open project screen.

#### Scenario: Result shown
- **WHEN** an action of the section finishes
- **THEN** the removed amounts and the freed space are shown and the figures are refreshed

#### Scenario: Other views refreshed
- **WHEN** the events of a project are cleared from the application Storage section
- **THEN** its tile, the tray counter and its project screen no longer show those events
