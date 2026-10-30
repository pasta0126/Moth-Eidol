## MODIFIED Requirements

### Requirement: Live display
The system SHALL show in a view the new entries of the selected source as they are emitted, each colored according to its level.

#### Scenario: New entry
- **WHEN** an entry from the selected source is emitted
- **THEN** it appears at the end of the view, colored by its level

#### Scenario: Multi-line entry
- **WHEN** an entry has continuation lines
- **THEN** it is shown as one item that can be expanded or collapsed

## ADDED Requirements

### Requirement: Level filter
The system SHALL allow filtering the displayed entries by level without affecting monitoring or alerts.

#### Scenario: Show only errors
- **WHEN** the user selects the error level filter
- **THEN** only entries of that level and above are displayed and the rest remain monitored
