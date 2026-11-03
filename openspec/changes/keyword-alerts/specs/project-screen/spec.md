## ADDED Requirements

### Requirement: Keyword editor in settings
The system SHALL provide in the project settings a keyword editor to add, remove and mark match-case keywords, showing for each keyword how many pending occurrences it has.

#### Scenario: Edit keywords
- **WHEN** the user adds a keyword in the project settings and saves
- **THEN** the project uses it for new entries

#### Scenario: Pending count per keyword
- **WHEN** a keyword has pending events
- **THEN** the editor shows the number next to it
