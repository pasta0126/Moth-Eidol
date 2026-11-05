## MODIFIED Requirements

### Requirement: Word wrap
The system SHALL provide a "Word wrap" checkbox in a view actions bar at the top-right corner of the tail entry list, enabled by default, that makes entries wrap to the width of the tail, and SHALL remember the choice across runs.

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

#### Scenario: Placement
- **WHEN** the user looks at the tail
- **THEN** the Word wrap checkbox is at the top-right corner of the entry list, not in the filter toolbar

## ADDED Requirements

### Requirement: View actions bar
The system SHALL show, at the top-right corner of the tail entry list, a compact actions bar holding the actions that act on the displayed text (word wrap and open folder), SHALL keep it from covering text or scrollbars, and SHALL let further actions be added to it.

#### Scenario: Actions next to the text
- **WHEN** the user looks at the tail
- **THEN** Word wrap and Open folder are in the bar above the entry list, right-aligned, and the filter toolbar holds only the source, level, pause, clear, max lines and scroll state

#### Scenario: Narrow window
- **WHEN** the window is narrow
- **THEN** the bar wraps within its own row without overlapping the list or the toolbar
