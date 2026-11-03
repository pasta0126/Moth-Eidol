## ADDED Requirements

### Requirement: Selectable text everywhere
The system SHALL allow the user to select and copy any read-only text shown in the application, including project names, tile texts, event titles, locations, messages, stack traces, context entries, occurrence details, settings labels, wizard review texts and the version footer.

#### Scenario: Copy an event title
- **WHEN** the user selects the title of an event group and copies it
- **THEN** the clipboard contains exactly the selected text

#### Scenario: Copy a stack trace
- **WHEN** the user selects part of a stack trace in an occurrence detail and copies it
- **THEN** the clipboard contains the selected lines in their original order

#### Scenario: Copy the version
- **WHEN** the user selects the version in the footer and copies it
- **THEN** the clipboard contains the selected text

### Requirement: Selection does not break navigation
The system SHALL keep clickable elements (tiles, event rows) working on a click and SHALL NOT activate them when the user drags to select text.

#### Scenario: Click a tile
- **WHEN** the user clicks a project tile without dragging
- **THEN** the project screen opens

#### Scenario: Drag over tile text
- **WHEN** the user drags over text inside a tile to select it
- **THEN** the text is selected and the project screen does not open

### Requirement: Editable fields unchanged
The system SHALL keep the native copy, cut and paste behavior of editable inputs.

#### Scenario: Paste into a field
- **WHEN** the user pastes text into the project name field
- **THEN** the text is inserted
