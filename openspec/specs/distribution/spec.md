# distribution Specification

## Purpose
TBD - created by archiving change distribution-packaging. Update Purpose after archive.

## Requirements

### Requirement: Self-contained packages
The system SHALL provide a script that publishes the application for Windows x64, macOS arm64, macOS x64 and Linux x64 as self-contained builds that run without any .NET runtime or SDK installed, and SHALL zip each build as `MothEidol-<version>-<platform>.zip` using the version from `Directory.Build.props`.

#### Scenario: Publish one platform
- **WHEN** the script is run for `win-x64`
- **THEN** `MothEidol-<version>-win-x64.zip` is produced, containing the executable and a README, and nothing needs to be installed to run it

#### Scenario: Publish all platforms
- **WHEN** the script is run without a platform
- **THEN** one zip per supported platform is produced

#### Scenario: Unknown platform
- **WHEN** the script is run with an unsupported platform
- **THEN** it fails with a message listing the supported ones and produces nothing

### Requirement: No elevated rights
The system SHALL run from any user-writable folder without administrator rights, SHALL NOT write outside the user's profile and temporary folders, and SHALL keep its data in the user's profile whether it runs from sources or from a package.

#### Scenario: Run unzipped
- **WHEN** a user unzips the package into their home folder and starts it
- **THEN** the application starts, creates its data in the user profile and needs no prompt for elevation

### Requirement: macOS bundle
The system SHALL package the macOS builds as `MothEidol.app` with the application icon and version in its Info.plist, ad-hoc signed so that it starts on Apple Silicon.

#### Scenario: Start the bundle
- **WHEN** the user opens `MothEidol.app` on macOS
- **THEN** the application starts with its icon and no terminal window

### Requirement: Run instructions
The system SHALL include in every package a README stating how to run it, where its data lives, and how to open an application that the OS flags as unsigned (SmartScreen on Windows, Gatekeeper on macOS).

#### Scenario: README present
- **WHEN** a package is opened
- **THEN** its README explains starting the application and bypassing the unsigned-application warning

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
The system SHALL document the steps to submit the package: Partner Center account and reserved name, package identity values, store listing, the "What's new in this version" text taken from the changelog entry of the submitted version, screenshots, age rating, privacy policy URL, certification kit validation and the certification wait.

#### Scenario: Checklist present
- **WHEN** a maintainer prepares a Store release
- **THEN** the documentation lists every item required before submission, including the "What's new in this version" text for that version

#### Scenario: What's new text for a submission
- **WHEN** a maintainer submits version X.Y.Z to the Store
- **THEN** the checklist tells them to take the "What's new in this version" text from the changelog entry of X.Y.Z
