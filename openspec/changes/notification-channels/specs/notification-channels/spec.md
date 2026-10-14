## Purpose

Defines how alerts are delivered to the user through independent, configurable channels that work on every platform without native OS notifications.

## ADDED Requirements

### Requirement: Channel chain
The system SHALL deliver each alert to every enabled notification channel independently, so a failure in one channel does not prevent delivery through the others.

#### Scenario: One channel fails
- **WHEN** an alert is delivered and one channel raises an error
- **THEN** the remaining enabled channels still deliver the alert and the failure is recorded

### Requirement: In-app toast
The system SHALL show an in-app toast for an alert, above other windows, without requiring the main window to be open, showing the rule, the source and the matching line.

#### Scenario: Alert with the window hidden
- **WHEN** an alert is produced while the main window is hidden in the tray
- **THEN** a toast is shown with the rule, source and line

#### Scenario: Toast dismissal
- **WHEN** the user dismisses a toast or its display time elapses
- **THEN** the toast disappears

### Requirement: Tray icon state and counter
The system SHALL reflect pending alerts on the tray icon with a distinct state and an unread counter, and SHALL clear them when the user opens the application or acknowledges them.

#### Scenario: Unread alerts
- **WHEN** alerts are delivered and not yet acknowledged
- **THEN** the tray icon shows the alert state and the number of unread alerts

#### Scenario: Acknowledge
- **WHEN** the user opens the history or acknowledges the alerts
- **THEN** the counter resets and the icon returns to its normal state

### Requirement: Sound
The system SHALL be able to play a sound when an alert is delivered, configurable on or off.

#### Scenario: Sound enabled
- **WHEN** sound is enabled and an alert is delivered
- **THEN** a sound is played once for that delivery

### Requirement: Channel configuration
The system SHALL allow enabling or disabling each channel independently and SHALL persist that configuration.

#### Scenario: Disable a channel
- **WHEN** the user disables the toast channel
- **THEN** no toasts are shown while the other enabled channels keep working

### Requirement: Extensible channels
The system SHALL expose a channel abstraction so additional channels (for example native OS notifications) can be added without changing alert production.

#### Scenario: New channel registered
- **WHEN** an additional channel implementation is registered
- **THEN** it receives alerts through the same delivery path as the built-in channels
