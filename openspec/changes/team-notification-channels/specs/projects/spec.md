## ADDED Requirements

### Requirement: Team channel configuration
The system SHALL persist per project the team channel settings (kind, destination, enabled, minimum level) and apply changes without restarting, keeping secrets in protected local storage.

#### Scenario: Edit while monitoring
- **WHEN** the user disables a team channel
- **THEN** subsequent alerts are no longer sent through it
