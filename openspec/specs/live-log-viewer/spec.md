# live-log-viewer Specification

## Purpose
Defines the live display of a log source's lines for active follow by the user.

## Requirements

### Requirement: Live display
The system SHALL show in a view the new entries of the selected source as they are emitted, each colored according to its level.

#### Scenario: New line
- **WHEN** an entry from the selected source is emitted
- **THEN** it appears at the end of the view, colored by its level

#### Scenario: Multi-line entry
- **WHEN** an entry has continuation lines
- **THEN** it is shown as one item that can be expanded or collapsed

### Requirement: Auto-scroll and pause
The system SHALL scroll the view automatically to the last line and SHALL allow pausing it, also suspending it when the user scrolls up.

#### Scenario: Manual pause
- **WHEN** the user pauses auto-scroll
- **THEN** new lines are appended without moving the view

#### Scenario: User scroll
- **WHEN** the user scrolls up in the view
- **THEN** auto-scroll is suspended until they return to the end or re-enable it

### Requirement: Bounded memory
The system SHALL keep a configurable maximum number of lines in the view, discarding the oldest.

#### Scenario: Limit reached
- **WHEN** the view exceeds the maximum number of lines
- **THEN** the oldest lines are discarded from the view without affecting monitoring

### Requirement: Level filter
The system SHALL allow filtering the displayed entries by level without affecting monitoring or alerts.

#### Scenario: Show only errors
- **WHEN** the user selects the error level filter
- **THEN** only entries of that level and above are displayed and the rest remain monitored
