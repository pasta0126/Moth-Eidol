## ADDED Requirements

### Requirement: Team channels in the chain
The system SHALL register webhook and email channels in the channel chain so they receive alerts through the same delivery path as the built-in channels, and a failure or slowness in a team channel SHALL NOT delay or prevent the local channels.

#### Scenario: Webhook unreachable
- **WHEN** an alert is delivered and the webhook endpoint is unreachable
- **THEN** the toast, tray and other local channels still deliver immediately and the webhook failure is recorded
