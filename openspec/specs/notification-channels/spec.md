# notification-channels Specification

## Purpose
Defines how alerts are delivered to the user through independent, configurable channels that work on every platform without native OS notifications.

## Requirements

### Requirement: Channel chain
The system SHALL deliver each alert to every enabled notification channel independently, so a failure in one channel does not prevent delivery through the others.

#### Scenario: One channel fails
- **WHEN** an alert is delivered and one channel raises an error
- **THEN** the remaining enabled channels still deliver the alert and the failure is recorded

### Requirement: In-app toast
The system SHALL show an in-app toast for an alert, above other windows, without requiring the main window to be open, showing the project, the level and the entry text.

#### Scenario: Alert with the window hidden
- **WHEN** an alert is produced while the main window is hidden in the tray
- **THEN** a toast is shown with the project, level and entry text

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
The system SHALL expose one application setting, "Alert sound", with the values with sound or silent, persisted across restarts. On Windows the setting SHALL decide whether the native notification plays the system notification sound (with sound by default) and no additional sound SHALL be played. On other platforms it SHALL decide whether the sound channel plays a sound once per delivery (silent by default).

#### Scenario: Sound enabled on Windows
- **WHEN** the setting is "with sound" and an alert is delivered on Windows
- **THEN** the native notification plays the system notification sound once

#### Scenario: Silent on Windows
- **WHEN** the setting is "silent" and an alert is delivered on Windows
- **THEN** the native notification is shown without any sound

#### Scenario: Sound enabled
- **WHEN** the setting is "with sound" and an alert is delivered on macOS or Linux
- **THEN** a sound is played once for that delivery

#### Scenario: Setting persists
- **WHEN** the user changes the setting and restarts the application
- **THEN** the chosen value is still applied

### Requirement: Channel configuration
The system SHALL allow enabling or disabling each channel independently, SHALL persist that configuration, and SHALL show the "Alert sound" setting in the application settings next to the channels.

#### Scenario: Disable a channel
- **WHEN** the user disables the toast channel
- **THEN** no toasts are shown while the other enabled channels keep working

#### Scenario: Change the sound setting
- **WHEN** the user switches "Alert sound" in the application settings
- **THEN** it applies from the next delivered alert, without restarting

### Requirement: Extensible channels
The system SHALL expose a channel abstraction so additional channels (for example native OS notifications) can be added without changing alert production.

#### Scenario: New channel registered
- **WHEN** an additional channel implementation is registered
- **THEN** it receives alerts through the same delivery path as the built-in channels

### Requirement: Native Windows notification
On Windows the system SHALL deliver each alert as a native Windows notification showing the project, the level and the entry headline, SHALL keep it in the Action Center, and SHALL bring the main window to the front when the user clicks it.

#### Scenario: Alert with the window hidden
- **WHEN** an alert is produced on Windows while the main window is hidden in the tray and the native channel is enabled
- **THEN** a Windows notification is shown with the project, level and entry headline

#### Scenario: Click on the notification
- **WHEN** the user clicks the native notification
- **THEN** the main window is shown and focused

#### Scenario: Native notification cannot be shown
- **WHEN** the native notification fails or the OS rejects it
- **THEN** the failure is recorded and the alert is shown as an in-app toast instead, even if that channel is disabled

### Requirement: Windows default channels
On Windows the native notification channel SHALL be enabled by default and the in-app toast channel SHALL be disabled by default; on other platforms the native channel SHALL NOT exist and the in-app toast stays enabled by default. A user choice already stored for a channel SHALL always win over these defaults.

#### Scenario: First run on Windows
- **WHEN** the application runs on Windows with no stored channel settings
- **THEN** alerts are delivered as native notifications and not as in-app toasts

#### Scenario: User re-enables the toast
- **WHEN** the user enables the in-app toast channel on Windows
- **THEN** alerts are delivered through both the native notification and the toast
