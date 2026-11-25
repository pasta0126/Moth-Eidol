# update-check Specification

## Purpose
Lets users of the Microsoft Store build learn that a newer version has been published in the Store, reminding them on every start until they update or dismiss that version, without the application ever updating itself.

## Requirements

### Requirement: Store version check at startup
In the build installed from the Microsoft Store, when "Check for new versions" is on, the system SHALL ask the Microsoft Store once per application start whether a version of the application newer than the running one is published. The check SHALL run in the background without delaying the startup or the monitoring, and SHALL NOT be repeated while the application keeps running.

#### Scenario: Newer version published
- **WHEN** the Store-installed application starts and the Store has a newer version published
- **THEN** the system learns that version number and announces it

#### Scenario: Already up to date
- **WHEN** the Store-installed application starts and the Store has no newer version
- **THEN** nothing is shown

#### Scenario: Running for days
- **WHEN** the application keeps running after the startup check and a newer version is published meanwhile
- **THEN** it is not announced until the next start

#### Scenario: Check disabled
- **WHEN** "Check for new versions" is off
- **THEN** the system does not contact the Store at startup and shows nothing

### Requirement: Silent failure of the check
When the version check cannot complete (no network, Store unavailable, the Store reports an error or no version number), the system SHALL show nothing, SHALL record the failure in the application log only, and SHALL try again at the next start.

#### Scenario: No network at startup
- **WHEN** the application starts without network access
- **THEN** no error is shown to the user and the application works normally

### Requirement: Only in the Store build
The system SHALL check for new versions and show the "Check for new versions" setting only when it runs as the package installed from the Microsoft Store. Zip installs on any platform SHALL NOT show the setting and SHALL NOT contact any server to check for versions.

#### Scenario: Zip install
- **WHEN** the application runs from the self-contained zip on Windows, macOS or Linux
- **THEN** the setting is not shown and no version check is made

### Requirement: Check for new versions setting
The system SHALL offer the general application setting "Check for new versions" in the application settings, on by default, remembered between runs. Turning it off SHALL also hide a banner shown in the current run; turning it on SHALL take effect at the next start.

#### Scenario: First run from the Store
- **WHEN** the Store-installed application runs for the first time
- **THEN** "Check for new versions" is on

#### Scenario: Turn it off
- **WHEN** the user turns "Check for new versions" off while an update banner is shown
- **THEN** the banner disappears and later starts make no check

### Requirement: Update banner on the dashboard
When a newer, not dismissed version is known, the system SHALL show a banner on the dashboard naming the new version, with three actions: "Open Store", "Not now" and "Don't remind me about this version". The banner SHALL stay visible while navigating back to the dashboard until the user acts on it, and SHALL NOT hide the version footer.

#### Scenario: Open Store
- **WHEN** the user clicks "Open Store"
- **THEN** the Microsoft Store opens on the application's page and the banner stays until the user dismisses it or restarts

#### Scenario: Not now
- **WHEN** the user clicks "Not now"
- **THEN** the banner disappears for the rest of this run and the version is announced again at the next start

#### Scenario: Store cannot be opened
- **WHEN** the Store page cannot be opened
- **THEN** the application does not fail and tells the user to update from the Microsoft Store

### Requirement: Startup notification
When a newer, not dismissed version is found, the system SHALL also show one Windows notification per start naming the version; clicking it SHALL bring the main window to the front on the dashboard. The notification SHALL be shown regardless of the alert notification channel settings, SHALL NOT play the alert sound and SHALL NOT change the tray counter. If the Windows notification cannot be shown, the banner alone SHALL suffice.

#### Scenario: New version at startup with the window hidden
- **WHEN** the application starts in the tray and a newer version is found
- **THEN** one Windows notification announces the version, with no sound and no change in the tray counter

#### Scenario: Click the notification
- **WHEN** the user clicks the update notification
- **THEN** the main window is shown on the dashboard with the update banner

### Requirement: Per-version dismissal
"Don't remind me about this version" SHALL remember that exact version between runs, hide the banner and stop announcing it at later starts. A version newer than the dismissed one SHALL be announced again. Without a dismissal, the version SHALL be announced at every start until the running version is equal to or newer than it.

#### Scenario: Dismiss a version
- **WHEN** the user dismisses version 1.12.0 and restarts while 1.12.0 is still the newest in the Store
- **THEN** no banner and no notification are shown

#### Scenario: Newer version after a dismissal
- **WHEN** version 1.12.0 was dismissed and the Store now publishes 1.13.0
- **THEN** 1.13.0 is announced at the next start

#### Scenario: Reminder without dismissal
- **WHEN** the user closed the banner with "Not now" or ignored it, and restarts the application
- **THEN** the same version is announced again

#### Scenario: Update installed
- **WHEN** the user installs the newer version from the Store and starts it
- **THEN** that version is not announced, since it is the running one

### Requirement: No automatic update
The system SHALL NOT download or install updates by itself; updating is always done by the user in the Microsoft Store.

#### Scenario: Newer version found
- **WHEN** a newer version is found
- **THEN** the application only announces it and keeps running the current version
