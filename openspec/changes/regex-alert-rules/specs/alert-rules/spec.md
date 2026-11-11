## ADDED Requirements

### Requirement: Regex rules
The system SHALL let the user define, per project, regular-expression rules, each with a name, a pattern, a kind (alert or ignore) and a case-sensitivity flag, SHALL reject invalid patterns, empty names and patterns longer than 500 characters, and SHALL allow at most 50 rules per project.

#### Scenario: Add a valid rule
- **WHEN** the user adds the alert rule "Oracle errors" with pattern `ORA-\d{5}`
- **THEN** it is saved and used from then on

#### Scenario: Invalid pattern
- **WHEN** the user enters the pattern `(unclosed`
- **THEN** the rule is rejected with a message explaining the syntax error and nothing is saved

### Requirement: Alert rules
The system SHALL treat an entry whose full text (including continuation lines) matches an enabled alert rule as alert-producing regardless of its level, and SHALL record the matched rule name with the event.

#### Scenario: Info entry matches an alert rule
- **WHEN** an info entry matches the alert rule "Order id" (`order \d{6}`)
- **THEN** an event is raised and shows the rule name

### Requirement: Ignore rules
The system SHALL NOT raise an event for an entry that matches an enabled ignore rule, even if its level or an alert rule or keyword would otherwise raise one, and SHALL still show the entry in the live tail.

#### Scenario: Known noise at error level
- **WHEN** an error entry matches the ignore rule "Health check timeout"
- **THEN** no event is raised and the entry remains visible in the tail

#### Scenario: Ignore wins over alert
- **WHEN** an entry matches both an ignore rule and an alert rule
- **THEN** no event is raised

### Requirement: Safe matching
The system SHALL bound the time spent matching each rule against an entry, SHALL treat a timeout as "no match", and SHALL record the failure and flag the rule as problematic in settings without interrupting monitoring.

#### Scenario: Pathological pattern
- **WHEN** a rule exceeds the matching time limit on an entry
- **THEN** the entry is processed as not matching that rule, monitoring continues and the rule is flagged

### Requirement: Rule tester
The system SHALL let the user try a rule against sample text and against the project's recent entries before saving, showing which entries match.

#### Scenario: Test against recent entries
- **WHEN** the user tests a pattern
- **THEN** the matching recent entries are listed without raising any event

### Requirement: Rule delivery
The system SHALL deliver events raised by alert rules through the enabled notification channels subject to the project's frequency policy, identifying the rule in the notification.

#### Scenario: Rule event in immediate mode
- **WHEN** an alert rule matches and the project uses immediate mode
- **THEN** a notification naming the rule is delivered, subject to the cooldown
