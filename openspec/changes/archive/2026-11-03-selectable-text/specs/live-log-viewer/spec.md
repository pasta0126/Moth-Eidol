## ADDED Requirements

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
