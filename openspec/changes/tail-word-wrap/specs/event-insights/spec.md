## ADDED Requirements

### Requirement: Word wrap in event detail
The system SHALL wrap the context entries and the occurrence text of an event detail, including its stack trace, to the available width according to the same word wrap option as the tail, enabled by default, and SHALL offer that option in the event detail.

#### Scenario: Default wrapping
- **WHEN** the user opens an event whose context or stack trace has long lines
- **THEN** the lines wrap within the detail without horizontal scrolling

#### Scenario: Disable from the detail
- **WHEN** the user clears the word wrap checkbox in the event detail
- **THEN** the context and occurrence lines show on one line each with horizontal scrolling, and the tail uses the same choice

#### Scenario: Shared choice
- **WHEN** the user disables word wrap in the tail and then opens an event
- **THEN** the event detail also has word wrap disabled
