## MODIFIED Requirements

### Requirement: Store submission checklist
The system SHALL document the steps to submit the package: Partner Center account and reserved name, package identity values, store listing, the "What's new in this version" text taken from the changelog entry of the submitted version, screenshots, age rating, privacy policy URL, certification kit validation and the certification wait. It SHALL also document the automated path: preparing the release with the release script, starting the Release workflow, approving the Store step, and the one-time setup it needs (Store identity in the repository configuration, an Entra ID app associated with the Partner Center account, the Store credentials as secrets and the protected environment with the maintainer as required reviewer). The manual steps SHALL remain documented as a fallback.

#### Scenario: Checklist present
- **WHEN** a maintainer prepares a Store release
- **THEN** the documentation lists every item required before submission, including the "What's new in this version" text for that version

#### Scenario: What's new text for a submission
- **WHEN** a maintainer submits version X.Y.Z to the Store
- **THEN** the checklist tells them to take the "What's new in this version" text from the changelog entry of X.Y.Z

#### Scenario: Automated release
- **WHEN** a maintainer wants to publish version X.Y.Z with the workflow
- **THEN** the documentation tells them the order: changelog entry, release script, Release workflow, approval of the Store step

#### Scenario: One-time setup
- **WHEN** a maintainer sets up the automated Store submission for the first time
- **THEN** the documentation lists every value, secret and setting to create and where each one comes from in Partner Center or Entra ID
