## MODIFIED Requirements

### Requirement: Project screen sections
The system SHALL show for the opened project an events section, a live tail section and a settings section, SHALL open on the events section, and SHALL allow switching between them without losing the tail.

#### Scenario: Open a project
- **WHEN** the user opens a project from its tile
- **THEN** the events section is shown first

#### Scenario: Switch sections
- **WHEN** the user moves from the tail to the settings and back
- **THEN** the tail still shows the lines that arrived in the meantime
