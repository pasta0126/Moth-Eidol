## Purpose

Defines the live display of a log source's lines for active follow by the user.

## ADDED Requirements

### Requirement: Live display
The system SHALL show in a view the new lines of the selected source as they are emitted.

#### Scenario: New line
- **WHEN** a line from the selected source is emitted
- **THEN** it appears at the end of the view

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
