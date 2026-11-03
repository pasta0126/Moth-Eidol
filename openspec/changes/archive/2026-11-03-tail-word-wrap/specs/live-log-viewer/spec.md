## ADDED Requirements

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
