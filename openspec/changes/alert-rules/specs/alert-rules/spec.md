## Purpose

Defines user-managed regex rules that evaluate log lines and produce alert events.

## ADDED Requirements

### Requirement: Rule definition
The system SHALL allow creating, editing, enabling, disabling and deleting rules, each with a name, a regular expression, a case-sensitivity option, a severity and a scope (all sources or specific ones).

#### Scenario: Create a rule
- **WHEN** the user creates a rule with a valid regular expression
- **THEN** the rule is saved, enabled and applied to subsequent lines

#### Scenario: Invalid expression
- **WHEN** the user saves a rule with an invalid regular expression
- **THEN** the rule is rejected and the error is shown to the user

### Requirement: Rule evaluation
The system SHALL evaluate every emitted line of a source against the enabled rules in scope and SHALL produce an alert event for each match.

#### Scenario: Line matches
- **WHEN** an emitted line matches an enabled rule in scope
- **THEN** an alert event is produced with the source, the line, the rule, its severity and the time

#### Scenario: Disabled rule
- **WHEN** a line matches only a disabled rule
- **THEN** no alert event is produced

### Requirement: Safe evaluation
The system SHALL bound the evaluation time of a rule so that a pathological expression cannot stall monitoring.

#### Scenario: Evaluation timeout
- **WHEN** a rule exceeds its evaluation time limit on a line
- **THEN** the line is skipped for that rule and the rule's error is recorded and shown

### Requirement: Persistence of rules and history
The system SHALL persist rules and an alert history across runs, and SHALL apply a configurable retention to the history.

#### Scenario: Restart
- **WHEN** the application restarts
- **THEN** rules and the alert history are restored

#### Scenario: Retention
- **WHEN** history entries exceed the configured retention
- **THEN** the oldest entries are removed

### Requirement: History view
The system SHALL show the alert history, newest first, with source, rule, severity, line and time.

#### Scenario: Browse history
- **WHEN** the user opens the history view
- **THEN** past alerts are listed newest first
