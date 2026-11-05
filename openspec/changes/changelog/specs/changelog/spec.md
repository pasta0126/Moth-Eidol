## Purpose
Defines the repository changelog, what each version entry contains and the rule that keeps it up to date, so people can see what improved between versions.

## ADDED Requirements

### Requirement: Changelog file
The repository SHALL contain a `CHANGELOG.md` at its root listing every released version, newest first, each with its release date and its changes grouped as Added, Changed and Fixed, written in user-facing language.

#### Scenario: Read the changelog
- **WHEN** a person opens `CHANGELOG.md`
- **THEN** they find each released version with its date and what was added, changed or fixed in plain language

### Requirement: Entry for every release
Each release SHALL have a changelog entry that is added before the version is bumped and tagged, so that no tagged version is left undocumented.

#### Scenario: Finishing a change
- **WHEN** a change is finished and a new version is prepared
- **THEN** its entry is added to the changelog before the version is bumped, committed and tagged

### Requirement: Not shown inside the application
The application SHALL NOT display the changelog or release notes itself.

#### Scenario: Application screens
- **WHEN** the user uses any screen of the application
- **THEN** no changelog or "What's new" screen is shown

### Requirement: Store text from the changelog
The system SHALL provide a way to extract the changelog entry of a given version as plain text for the Store "What's new in this version" field, and SHALL fail with a clear message when the text exceeds the Store length limit or the version has no entry.

#### Scenario: Extract a version
- **WHEN** the helper is run for a version that has a changelog entry within the limit
- **THEN** it prints that entry as plain text ready to paste in Partner Center

#### Scenario: Entry too long or missing
- **WHEN** the helper is run for a version whose entry is missing or exceeds the Store limit
- **THEN** it fails and says which of the two problems occurred
