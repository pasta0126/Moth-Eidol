# project-wizard Specification

## Purpose
TBD - created by archiving change project-setup-wizard. Update Purpose after archive.

## Requirements

### Requirement: Guided steps
The system SHALL guide project creation through ordered steps: name, log location, alerts and review, showing the current step and total, and SHALL allow going back, cancelling, and advancing only when the current step is valid.

#### Scenario: Advance with a valid step
- **WHEN** the user fills a valid name and chooses next
- **THEN** the log location step is shown

#### Scenario: Invalid step blocks advancing
- **WHEN** the name is empty or already used by another project
- **THEN** the wizard stays on the step and shows why

#### Scenario: Go back keeps values
- **WHEN** the user goes back from a later step
- **THEN** the values already entered are still there

#### Scenario: Cancel
- **WHEN** the user cancels at any step
- **THEN** the dashboard is shown and no project or source has been created

### Requirement: Log location
The system SHALL let the user add one or more log sources in the location step, each as a file or as a folder with a file-name pattern, choosing the path by typing or browsing, and SHALL require at least one source before advancing.

#### Scenario: Add a folder with a pattern
- **WHEN** the user picks a folder and enters `*.log`
- **THEN** the source is added to the wizard's list

#### Scenario: Several sources
- **WHEN** the user adds a second source
- **THEN** both are listed and either can be removed before advancing

#### Scenario: No source
- **WHEN** the user tries to advance without sources
- **THEN** the wizard stays on the step and explains that one source is needed

### Requirement: Location check
The system SHALL check a source as soon as it is entered and show whether the path exists and is readable, the files matching the pattern, the newest one that would be followed, and a sample of its latest entries with their detected levels, and SHALL warn without blocking when the location is valid but yields no entries or is not recognized as Serilog text.

#### Scenario: Readable folder with matches
- **WHEN** the folder exists and contains files matching the pattern
- **THEN** the matching files, the newest one and a sample of recognized entries with levels are shown

#### Scenario: Path missing or unreadable
- **WHEN** the path does not exist or cannot be read by the OS user
- **THEN** the reason is shown and the source cannot be added

#### Scenario: No matching files
- **WHEN** the folder is readable but nothing matches the pattern
- **THEN** a warning says so and the user may still add the source, since the files may appear later

#### Scenario: Unrecognized format
- **WHEN** the newest file has lines but none is recognized as an entry
- **THEN** a warning says the format is not recognized and the user may still add the source

### Requirement: Alert options
The system SHALL ask in the alerts step whether warnings raise alerts and which frequency applies (immediate with cooldown, or periodic summary) with its minutes, starting from the defaults, and SHALL explain each option in plain words.

#### Scenario: Defaults
- **WHEN** the alerts step opens
- **THEN** warnings are off and the mode is immediate with the default cooldown

#### Scenario: Invalid minutes
- **WHEN** the minutes value is out of the allowed range
- **THEN** the wizard stays on the step and shows the allowed range

### Requirement: Review and create
The system SHALL show a review of all values in the last step and, on confirmation, SHALL create the project with its sources and alert settings as a single operation, so that either everything is saved or nothing is, and SHALL then open the new project on its events section.

#### Scenario: Create
- **WHEN** the user confirms the review
- **THEN** the project, its sources and alert settings exist, monitoring starts and its events section is shown

#### Scenario: Name taken meanwhile
- **WHEN** creation fails because the name is now used
- **THEN** nothing is saved, the wizard returns to the name step and shows the error
