## ADDED Requirements

### Requirement: Raw view of the tail
The system SHALL provide a "Raw" toggle in the tail toolbar that shows the entries visible after the level filter as plain text without styles, in a read-only text area with the usual text selection behavior (drag across lines, double click on a word, triple click on a line, Shift+click, select all, copy), SHALL take a snapshot when Raw is turned on, SHALL offer a Refresh action to take a new one, SHALL follow the word wrap option, and SHALL return to the styled live view when Raw is turned off.

#### Scenario: Turn Raw on
- **WHEN** the user turns Raw on
- **THEN** the visible entries are shown as plain text, one after another with their continuation lines, in display order

#### Scenario: Select across lines
- **WHEN** the user presses on one line in Raw and drags over several lines, and then copies
- **THEN** the clipboard contains the selected text with the original line breaks

#### Scenario: Entries arrive while in Raw
- **WHEN** new entries arrive while Raw is on
- **THEN** the text and the selection do not change, the entries keep being monitored, and Refresh includes them

#### Scenario: Level filter in Raw
- **WHEN** the level filter shows only errors and the user turns Raw on
- **THEN** only the error entries are in the plain text

#### Scenario: Turn Raw off
- **WHEN** the user turns Raw off
- **THEN** the styled view is shown again with the current entries

#### Scenario: Very large tail
- **WHEN** more than 20,000 lines are visible and the user turns Raw on
- **THEN** the last 20,000 lines are shown with a note that the text was truncated
