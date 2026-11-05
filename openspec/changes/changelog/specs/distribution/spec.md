## MODIFIED Requirements

### Requirement: Store submission checklist
The system SHALL document the steps to submit the package: Partner Center account and reserved name, package identity values, store listing, the "What's new in this version" text taken from the changelog entry of the submitted version, screenshots, age rating, privacy policy URL, certification kit validation and the certification wait.

#### Scenario: Checklist present
- **WHEN** a maintainer prepares a Store release
- **THEN** the documentation lists every item required before submission, including the "What's new in this version" text for that version

#### Scenario: What's new text for a submission
- **WHEN** a maintainer submits version X.Y.Z to the Store
- **THEN** the checklist tells them to take the "What's new in this version" text from the changelog entry of X.Y.Z
