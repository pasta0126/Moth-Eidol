## Purpose

Defines configurable anti-spam behavior so repeated matches do not flood the user with alerts.

## ADDED Requirements

### Requirement: Configurable rate limit
The system SHALL limit the number of alerts delivered per rule within a time window, configurable globally and overridable per rule, with sensible defaults.

#### Scenario: Under the limit
- **WHEN** a rule produces fewer alerts than the limit within the window
- **THEN** each alert is delivered individually

#### Scenario: Over the limit
- **WHEN** a rule produces more alerts than the limit within the window
- **THEN** alerts beyond the limit are suppressed from delivery

### Requirement: Aggregated summary
The system SHALL deliver a single summary alert stating how many alerts were suppressed once a throttled window ends.

#### Scenario: Suppression summary
- **WHEN** a window ends after suppressing alerts
- **THEN** one summary alert such as "+37 more" is delivered for that rule

### Requirement: Complete history regardless of throttling
The system SHALL record every match in the alert history, including those suppressed from delivery.

#### Scenario: Suppressed match
- **WHEN** an alert is suppressed by throttling
- **THEN** it is still recorded in the history and marked as suppressed

### Requirement: Throttling can be disabled
The system SHALL allow disabling throttling globally or per rule.

#### Scenario: Throttling off
- **WHEN** throttling is disabled for a rule
- **THEN** every match of that rule is delivered individually
