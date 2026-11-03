## ADDED Requirements

### Requirement: Copy from the event detail
The system SHALL let the user select text within the context entries and the occurrence text of an event detail, SHALL let the user copy the selected text with the standard shortcut and a context menu, and SHALL offer copying all context entries plus the occurrence text.

#### Scenario: Copy part of a context entry
- **WHEN** the user selects text in a context entry and copies it
- **THEN** the clipboard contains the selected text

#### Scenario: Copy context and occurrence
- **WHEN** the user chooses "Copy all" in the event detail
- **THEN** the clipboard contains the context entries followed by the occurrence text in display order

#### Scenario: Selection with wrapping
- **WHEN** the lines are wrapped and the user selects across wrapped lines
- **THEN** the copied text contains the original line breaks only, not the visual wraps
