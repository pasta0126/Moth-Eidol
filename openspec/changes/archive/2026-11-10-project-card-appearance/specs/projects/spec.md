## ADDED Requirements

### Requirement: Project accent color
The system SHALL let the user choose, in the project's settings, an accent color from a palette of 16 colors or by typing a hex value (`#RRGGBB`, `RRGGBB` or `#RGB`), and SHALL offer a way to return to the default color. An invalid hex value SHALL be rejected with a clear message and not saved.

#### Scenario: Pick from the palette
- **WHEN** the user selects one of the 16 swatches and saves
- **THEN** the project's accent is that color and its tile reflects it

#### Scenario: Type a hex value
- **WHEN** the user types `3fa7d6` in the hex field
- **THEN** it is normalized to `#3FA7D6`, shown as selected, and saved

#### Scenario: Invalid hex
- **WHEN** the user types `#12ZZ99`
- **THEN** an error is shown next to the field and the previous color is kept

#### Scenario: Back to default
- **WHEN** the user chooses the default color
- **THEN** the project has no accent stored and uses the default accent

### Requirement: Project order
The system SHALL store a position for each project; new projects SHALL be placed last, and deleting a project SHALL NOT disturb the relative order of the others.

#### Scenario: New project
- **WHEN** a project is created
- **THEN** it appears after all existing projects
