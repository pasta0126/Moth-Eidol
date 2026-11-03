## ADDED Requirements

### Requirement: Project keywords
The system SHALL let the user define, per project, a list of keywords (words or phrases), SHALL trim them, SHALL reject empty ones, ones longer than 200 characters and case-insensitive duplicates, and SHALL allow at most 50 per project.

#### Scenario: Add a keyword
- **WHEN** the user adds "payment declined" to a project
- **THEN** it is saved and used from then on

#### Scenario: Duplicate keyword
- **WHEN** the user adds "Timeout" and "timeout" already exists
- **THEN** the second is rejected with a message

#### Scenario: Remove a keyword
- **WHEN** the user removes a keyword
- **THEN** new entries no longer match it and existing events are kept

### Requirement: Keyword matching
The system SHALL match keywords as plain text contained anywhere in the full entry text, including continuation lines, ignoring case unless the keyword is marked match-case, regardless of the entry level.

#### Scenario: Case-insensitive match
- **WHEN** the keyword is "timeout" and an entry contains "Request TIMEOUT"
- **THEN** the entry matches

#### Scenario: Match-case keyword
- **WHEN** the keyword "ORA-" is marked match-case and an entry contains "ora-00942"
- **THEN** the entry does not match

#### Scenario: Match in a stack trace
- **WHEN** the keyword appears only in a continuation line
- **THEN** the entry matches

#### Scenario: Several keywords
- **WHEN** an entry matches two keywords
- **THEN** one occurrence is recorded listing both keywords

### Requirement: Keyword notifications
The system SHALL deliver keyword events through the enabled notification channels subject to the project's frequency policy, and SHALL identify the keyword in the notification.

#### Scenario: Keyword in immediate mode
- **WHEN** an info entry matches a keyword and the project is not in cooldown
- **THEN** a notification naming the project and the keyword is delivered

#### Scenario: Keyword during cooldown
- **WHEN** keyword matches arrive during the cooldown
- **THEN** they are counted and summarized when the cooldown ends

### Requirement: Keyword persistence
The system SHALL persist keywords with the project across runs.

#### Scenario: Restart
- **WHEN** the application restarts
- **THEN** the project's keywords are restored
