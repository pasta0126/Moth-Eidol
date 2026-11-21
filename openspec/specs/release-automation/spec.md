# release-automation Specification

## Purpose
Automates building, testing and publishing the Windows release of Moth-Eidol, while the maintainer keeps the decision of which version is published and when.

## Requirements

### Requirement: Release preparation script
The system SHALL provide a script that, given a release version `x.y.z`, sets that version in `Directory.Build.props`, commits it, creates the annotated tag `vX.Y.Z` and pushes the commit and the tag, and SHALL refuse with a message and change nothing when any of these holds: the version is not of the form `x.y.z`; it is not greater than the `x.y.z` part of the current version; the working tree has uncommitted changes; the current branch is not `main`; the tag already exists; or `CHANGELOG.md` has no entry `## [x.y.z] - YYYY-MM-DD` for it.

#### Scenario: Prepare a release
- **WHEN** the current version is `1.10.0.3`, the changelog has a dated `1.11.0` entry and the script is run with `1.11.0` on a clean `main`
- **THEN** `Directory.Build.props` holds `1.11.0`, a commit and the tag `v1.11.0` are created and both are pushed

#### Scenario: Missing changelog entry
- **WHEN** the script is run with a version that has no dated changelog entry
- **THEN** it fails saying the entry is missing, and the version, commits and tags are unchanged

#### Scenario: Version not greater
- **WHEN** the current version is `1.10.0.3` and the script is run with `1.10.0`
- **THEN** it fails saying the version must be greater than `1.10.0`, and nothing changes

#### Scenario: Dirty tree or other branch
- **WHEN** the script is run with uncommitted changes or outside `main`
- **THEN** it fails naming the problem, and nothing changes

### Requirement: Publication only on demand
Publishing a release SHALL happen only when the maintainer starts the Release workflow by hand for a given version; pushing a commit or a tag SHALL NOT publish anything.

#### Scenario: Tag pushed
- **WHEN** the tag `v1.11.0` is pushed
- **THEN** no GitHub Release is created and nothing is sent to the Store

#### Scenario: Manual start
- **WHEN** the maintainer starts the Release workflow with version `1.11.0`
- **THEN** the release of `1.11.0` is built and published as described by the other requirements

### Requirement: Release preconditions
The Release workflow SHALL stop before building anything, with a message naming the failed check, unless the tag `vX.Y.Z` exists, the `Directory.Build.props` at that tag holds exactly `x.y.z`, and the changelog at that tag has an entry for `x.y.z` that can be extracted as Store text within the Store length limit. Everything the workflow builds SHALL come from the tagged commit.

#### Scenario: Tag missing
- **WHEN** the workflow is started for a version without a tag
- **THEN** it stops saying the tag does not exist, and nothing is published

#### Scenario: Development version at the tag
- **WHEN** the tagged `Directory.Build.props` holds `1.11.0.2` or another version than the requested one
- **THEN** it stops saying the version does not match, and nothing is published

#### Scenario: Changelog entry too long
- **WHEN** the changelog entry of the version exceeds the Store limit
- **THEN** it stops saying the entry is too long, and nothing is published

### Requirement: Windows release artifacts
The Release workflow SHALL build and test the tagged commit on Windows and SHALL produce `MothEidol-<version>-win-x64.zip` and the Store package `MothEidol-<version>-win-x64.msix`, taking the Store identity from repository configuration rather than from values typed at each run, and SHALL keep both files as downloadable artifacts of the run. If a test fails, nothing is published.

#### Scenario: Successful build
- **WHEN** the preconditions pass and the tests succeed
- **THEN** the run provides the zip and the unsigned Store MSIX of that version

#### Scenario: Store identity not configured
- **WHEN** the Store identity is missing from the repository configuration
- **THEN** the workflow fails saying which identity value is missing, and nothing is published

### Requirement: Public GitHub Release
The Release workflow SHALL publish a public GitHub Release named and tagged `vX.Y.Z` whose notes are the changelog entry of that version and which has the win-x64 zip attached. The Store MSIX SHALL NOT be attached, because it cannot be installed outside the Store. Running the workflow again for a version whose release already exists SHALL update its notes and replace the zip instead of failing.

#### Scenario: Release published
- **WHEN** the workflow finishes for `1.11.0`
- **THEN** the repository's Releases page shows `v1.11.0` with the `1.11.0` changelog entry and `MothEidol-1.11.0-win-x64.zip` to download

#### Scenario: Run again
- **WHEN** the workflow is run again for `1.11.0`
- **THEN** the existing `v1.11.0` release is updated with the current notes and zip, and no duplicate is created

### Requirement: Store submission on approval
When the maintainer asks for it at start, the Release workflow SHALL, after the maintainer approves the step, upload the Store MSIX to Partner Center as a new submission of the app, set its "What's new in this version" text to the changelog entry of the version, and submit it for certification. Without that request the workflow SHALL NOT contact the Store. If the step is rejected, fails or the Store credentials are missing, the GitHub Release SHALL stay published and the run SHALL say that the Store was not updated.

#### Scenario: Submit to the Store
- **WHEN** the workflow is started with Store submission requested and the maintainer approves the step
- **THEN** a submission with the new package and the changelog text is sent to certification

#### Scenario: Store not requested
- **WHEN** the workflow is started without Store submission
- **THEN** only the GitHub Release is published and Partner Center is not contacted

#### Scenario: Approval rejected
- **WHEN** the maintainer rejects the Store step
- **THEN** nothing is sent to the Store and the GitHub Release remains

#### Scenario: Credentials missing
- **WHEN** the Store step runs without the Store credentials configured
- **THEN** it fails saying which credential is missing, before contacting the Store

### Requirement: No secrets in the repository
Store credentials SHALL be kept only as repository or environment secrets, SHALL NOT be committed nor written to the run logs or artifacts, and SHALL be usable only by the Store submission step.

#### Scenario: Inspect a run
- **WHEN** someone reads the logs and artifacts of a Release run
- **THEN** no Store credential appears in them
