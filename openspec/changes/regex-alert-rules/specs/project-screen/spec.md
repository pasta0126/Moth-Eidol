## ADDED Requirements

### Requirement: Rules editor in settings
The system SHALL provide in the project settings an editor to add, edit, enable/disable, reorder and remove alert and ignore rules, with the rule tester.

#### Scenario: Disable a rule
- **WHEN** the user disables a rule
- **THEN** new entries stop being evaluated against it and existing events are kept
