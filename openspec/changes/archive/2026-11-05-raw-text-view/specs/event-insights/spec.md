## ADDED Requirements

### Requirement: Raw view of the event detail
The system SHALL provide a "Raw" toggle in the event detail that shows the context entries followed by the occurrence text as plain text without styles, in a read-only text area with the usual text selection behavior, following the word wrap option, and returning to the styled detail when turned off.

#### Scenario: Turn Raw on
- **WHEN** the user turns Raw on in an event detail
- **THEN** the context entries and the occurrence text, including the stack trace, are shown as plain text in display order

#### Scenario: Select and copy anything
- **WHEN** the user selects any part of the plain text, including several lines, and copies it
- **THEN** the clipboard contains exactly the selected text

#### Scenario: Another occurrence
- **WHEN** Raw is on and the user selects another occurrence
- **THEN** the plain text shows that occurrence

### Requirement: Copy button per occurrence
The system SHALL show a Copy button on each item of the Occurrences list that copies the whole detail of that occurrence, which is the context entries before the error followed by the full message and stack trace, and SHALL confirm the copy.

#### Scenario: Copy an occurrence
- **WHEN** the user presses the Copy button of an occurrence
- **THEN** the clipboard contains its context entries followed by its full message and stack trace, in display order, and a confirmation is shown

#### Scenario: Button does not change the selection
- **WHEN** the user presses the Copy button of an occurrence other than the selected one
- **THEN** that occurrence is copied and the displayed detail does not change
