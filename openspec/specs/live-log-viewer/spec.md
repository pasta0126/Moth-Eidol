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

### Requirement: Copy from the tail
The system SHALL let the user select text within a tail entry, SHALL let the user copy the selected entries (full text including continuation lines) with the standard shortcut and a context menu, and SHALL offer copying all entries currently visible after the level filter.

#### Scenario: Copy selected text of a line
- **WHEN** the user selects text inside a tail line and copies it
- **THEN** the clipboard contains the selected text

#### Scenario: Copy selected entries
- **WHEN** the user selects several entries and chooses Copy
- **THEN** the clipboard contains their full text, one entry after another, in display order

#### Scenario: Copy visible entries
- **WHEN** the level filter shows only errors and the user chooses "Copy all visible"
- **THEN** the clipboard contains only the displayed error entries

#### Scenario: Selection while auto-scrolling
- **WHEN** new entries arrive while the user has text selected
- **THEN** the selected text stays copyable

### Requirement: Word wrap
The system SHALL provide a "Word wrap" checkbox in the tail toolbar, enabled by default, that makes entries wrap to the width of the tail, and SHALL remember the choice across runs.

#### Scenario: Default
- **WHEN** the user opens the tail for the first time
- **THEN** word wrap is enabled and long entries fill the window width without horizontal scrolling

#### Scenario: Wrapped stack trace
- **WHEN** word wrap is enabled and an entry has long continuation lines
- **THEN** each line wraps within the tail and the message text stays aligned after the time and level columns

#### Scenario: Disable wrap
- **WHEN** the user clears the checkbox
- **THEN** entries show on one line each and a horizontal scrollbar appears for long lines

#### Scenario: Window resize
- **WHEN** word wrap is enabled and the window is resized
- **THEN** the entries re-wrap to the new width

#### Scenario: Auto-scroll with wrapping
- **WHEN** auto-scroll is active and a long wrapped entry arrives
- **THEN** the end of that entry is visible

#### Scenario: Remembered choice
- **WHEN** the user disables word wrap and restarts the application
- **THEN** word wrap is still disabled
