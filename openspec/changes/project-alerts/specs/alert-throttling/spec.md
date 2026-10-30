## Purpose

Defines the per-project frequency policy so repeated alerts do not flood the user: immediate delivery with a cooldown, or a periodic summary.

## ADDED Requirements

### Requirement: Immediate delivery with cooldown
The system SHALL, for a project in immediate mode, deliver the first alert at once and group further alerts of that project until the cooldown elapses.

#### Scenario: First alert
- **WHEN** an alert is produced and the project is not in cooldown
- **THEN** it is delivered at once and the cooldown starts

#### Scenario: Alerts during cooldown
- **WHEN** more alerts are produced for the project during the cooldown
- **THEN** they are not delivered individually and are counted

#### Scenario: Cooldown ends with suppressed alerts
- **WHEN** the cooldown ends after suppressing alerts
- **THEN** one summary alert such as "+37 more" is delivered for that project

### Requirement: Periodic summary
The system SHALL, for a project in summary mode, deliver one aggregated alert per interval stating the number of alerts per level and a sample of the entries.

#### Scenario: Alerts in the interval
- **WHEN** an interval ends and alerts were produced during it
- **THEN** one summary alert is delivered for the project

#### Scenario: No alerts in the interval
- **WHEN** an interval ends without alerts
- **THEN** nothing is delivered

### Requirement: Policy per project
The system SHALL apply the frequency policy independently to each project, using global defaults that each project can override.

#### Scenario: Two projects
- **WHEN** two projects produce alerts at the same time
- **THEN** each follows its own mode, interval and cooldown

### Requirement: Complete history regardless of throttling
The system SHALL record every alert in the history, including those not delivered individually, marked as suppressed.

#### Scenario: Suppressed alert
- **WHEN** an alert is suppressed by the frequency policy
- **THEN** it is still recorded in the history and marked as suppressed
