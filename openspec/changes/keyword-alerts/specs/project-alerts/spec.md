## MODIFIED Requirements

### Requirement: Level-based alert production
The system SHALL treat as alert-level every entry of an enabled project whose level is error or above, SHALL treat warning entries as alert-level only when the project has warning alerts enabled, SHALL treat as alert-producing any entry matching a keyword of the project whatever its level, and SHALL NOT treat other information level or below, or unknown level, as alert-level.

#### Scenario: Error entry
- **WHEN** an entry of level error is emitted by an enabled project
- **THEN** an alert event is produced with the project, source, entry text, level and time

#### Scenario: Warning with warnings disabled
- **WHEN** a warning entry that matches no keyword is emitted by a project with warning alerts disabled
- **THEN** no alert event is produced and the entry is still shown in the tail

#### Scenario: Warning with warnings enabled
- **WHEN** a warning entry is emitted by a project with warning alerts enabled
- **THEN** an alert event is produced

#### Scenario: Information entry
- **WHEN** an information entry that matches no keyword is emitted
- **THEN** no alert event is produced

#### Scenario: Information entry with keyword
- **WHEN** an information entry matching a keyword of the project is emitted
- **THEN** an alert event is produced with level information and the matched keyword

#### Scenario: Disabled project
- **WHEN** an entry is emitted by a disabled project
- **THEN** no alert event is produced, even if it matches a keyword

## ADDED Requirements

### Requirement: Keyword events in the events section
The system SHALL show keyword events in the project's events section as groups with pending/seen state, marked with the matched keyword, SHALL keep them apart from level-based groups, and SHALL count them in the tile's pending number with a notice status when only information-level keyword events are pending.

#### Scenario: Pending keyword event
- **WHEN** an information entry matches a keyword
- **THEN** the events section shows a pending group marked with that keyword and the tile shows the pending count

#### Scenario: Only keyword events pending
- **WHEN** a project has only pending information-level keyword groups
- **THEN** its tile shows the notice status, not warning or error

#### Scenario: Same message as an error
- **WHEN** an error and a keyword-matched info entry share the same normalized message
- **THEN** they belong to different groups
