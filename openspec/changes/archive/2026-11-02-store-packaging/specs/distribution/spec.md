## ADDED Requirements

### Requirement: Microsoft Store package
The system SHALL provide a script that builds an MSIX package for Windows x64 from the self-contained build, named `MothEidol-<version>-win-x64.msix`, whose manifest declares full trust and whose four-part version is the `Directory.Build.props` version followed by `.0`.

#### Scenario: Build the package
- **WHEN** the script is run on a Windows host with the Windows SDK installed
- **THEN** `MothEidol-<version>-win-x64.msix` is produced with package version `<major>.<minor>.<patch>.0`

#### Scenario: Unsupported host
- **WHEN** the script is run on a host without the Windows SDK tools
- **THEN** it fails with a message naming the missing tool and produces nothing

#### Scenario: Missing store identity
- **WHEN** the package identity and publisher have not been provided
- **THEN** the script builds only a test package signed with a local test certificate and states that it cannot be submitted to the Store

### Requirement: Store package behavior parity
The system SHALL behave in the packaged build like in the zip build with regard to reading log files and network shares with the OS user's permissions and keeping its data in the user's profile.

#### Scenario: Read a network share
- **WHEN** a project points at a share reachable by the OS user and the packaged app runs
- **THEN** its log files are read and followed as in the zip build

### Requirement: Store submission checklist
The system SHALL document the steps to submit the package: Partner Center account and reserved name, package identity values, store listing, screenshots, age rating, privacy policy URL, certification kit validation and the certification wait.

#### Scenario: Checklist present
- **WHEN** a maintainer prepares a Store release
- **THEN** the documentation lists every item required before submission
