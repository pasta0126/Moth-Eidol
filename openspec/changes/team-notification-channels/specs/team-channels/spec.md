## ADDED Requirements

### Requirement: Webhook channel
The system SHALL, when a project has an enabled webhook, send an HTTPS POST with a JSON body containing the project name, level, readable title, message, timestamp, occurrence count and the matched keyword or rule when any, with a request timeout, and SHALL offer a preset body for Slack-compatible and Teams-compatible incoming webhooks.

#### Scenario: Alert posted to a webhook
- **WHEN** an error event is delivered and the project has an enabled webhook
- **THEN** a POST with the event JSON is sent to the configured URL

#### Scenario: Non-HTTPS URL
- **WHEN** the user enters an `http://` URL that is not localhost
- **THEN** it is rejected with a message, unless the user explicitly confirms an insecure destination

### Requirement: Email channel
The system SHALL, when a project has an enabled email destination, send a plain-text and HTML email through the configured SMTP server to the listed recipients with the project, level, title and summary of the event, using TLS.

#### Scenario: Alert sent by email
- **WHEN** an error event is delivered and email is enabled for the project
- **THEN** an email with the event summary is sent to the recipients

### Requirement: Per-project configuration
The system SHALL let the user configure team channels per project (destination, enabled, minimum level) and SHALL apply the project's frequency policy so immediate mode honors the cooldown and summary mode sends one periodic digest.

#### Scenario: Summary mode
- **WHEN** the project uses periodic summary
- **THEN** one digest is sent per period instead of one message per event

#### Scenario: Minimum level
- **WHEN** the channel's minimum level is error and a warning event occurs
- **THEN** nothing is sent through that channel

### Requirement: Credentials stay local
The system SHALL store webhook URLs that act as secrets and SMTP credentials only on the user's machine, protected with the OS credential facilities where available, and SHALL NOT include them in logs or in exported project configuration.

#### Scenario: Export with a webhook
- **WHEN** a project with a webhook is exported
- **THEN** the exported file does not contain the webhook URL or any credential

### Requirement: Test send
The system SHALL provide a "Send test" action for each team channel that sends a clearly marked test message and reports success or the failure reason.

#### Scenario: Wrong credentials
- **WHEN** the user runs the test with invalid SMTP credentials
- **THEN** the failure reason is shown and nothing is saved as working

### Requirement: Failure handling
The system SHALL retry a failed delivery a bounded number of times with backoff, SHALL then drop it recording the failure, SHALL show a delivery-failure indicator for the project and SHALL NOT retry indefinitely or accumulate an unbounded queue.

#### Scenario: Persistent failure
- **WHEN** the endpoint keeps failing after the retries
- **THEN** the delivery is dropped, the failure is recorded and the project settings show the channel as failing

### Requirement: Disclosure of local-only operation
The system SHALL state in the team channel settings that notifications are sent only while this application is running and that several teammates monitoring the same project may send duplicates.

#### Scenario: Reading the settings
- **WHEN** the user opens the team channels section
- **THEN** a short note explains the limitation
