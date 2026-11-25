## MODIFIED Requirements

### Requirement: No data collection
The system SHALL NOT collect or transmit telemetry, usage data or feedback by itself, and the privacy policy SHALL state that the links only open the browser. The only connection the system makes on its own SHALL be the Microsoft Store version check of the Store build, which SHALL send no project, log, setting or personal data, and the privacy policy SHALL describe it and how to turn it off.

#### Scenario: Privacy policy
- **WHEN** a user reads the privacy policy
- **THEN** it states that the application only opens links in the browser and sends no data on its own

#### Scenario: Version check in the privacy policy
- **WHEN** a user reads the privacy policy
- **THEN** it explains that the Store build asks the Microsoft Store at startup whether a newer version exists, that nothing about their projects or logs is sent, and that "Check for new versions" turns it off
