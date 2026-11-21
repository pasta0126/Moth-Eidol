## Why

Releasing a version is today a sequence of manual steps spread over two machines: bump the version, tag and push on macOS, then on a Windows host (or a hand-filled `Build MSIX` run) build the zip and the MSIX, paste the changelog text, upload to Partner Center and submit. There is also no public download outside the Store. Automating the Windows deployment makes releases repeatable, and every release is tested on Windows before it ships, while the maintainer keeps the decision of what and when to publish.

## What Changes

- New local release script: given the release version `x.y.z`, check that the working tree is clean, the branch is `main`, the version is greater than the current one (the development fourth part is dropped) and `CHANGELOG.md` has a dated entry for it; then set the version in `Directory.Build.props`, commit, create the tag `vX.Y.Z` and push both. A tag alone triggers no publication.
- New on-demand `Release` workflow (manual trigger, version as input), Windows only: it refuses to run unless the tag `vX.Y.Z` exists, the tagged `Directory.Build.props` holds exactly `x.y.z` and the changelog entry exists within the Store limit. It builds and tests from the tag, produces the win-x64 zip and the Store MSIX (identity from repository variables instead of typed inputs), and publishes a public GitHub Release `vX.Y.Z` with the changelog entry as notes and the zip attached.
- Optional Store submission step in the same workflow, requested by an input flag and held for the maintainer's approval in a protected GitHub environment: it uploads the MSIX to Partner Center, sets "What's new in this version" from the changelog entry and submits it for certification.
- The manual `Build MSIX` workflow is replaced by the `Release` workflow.
- `packaging/SUBMISSION.md` documents the automated path and the one-time setup (repository variables, Entra ID app linked in Partner Center, secrets, protected environment); the manual steps stay documented as fallback.
- Out of scope: a build-and-test run on every push (can be added later), macOS and Linux artifacts in CI, macOS signing and notarization, automating screenshots or the rest of the Store listing.

## Capabilities

### New Capabilities
- `release-automation`: the local release script, the on-demand Release workflow (checks, zip, MSIX, GitHub Release) and the gated Store submission.

### Modified Capabilities
- `distribution`: the Store submission checklist also documents the automated submission and its one-time setup.

## Impact

- New: `.github/workflows/release.yml`, `scripts/release.sh`.
- Removed: `.github/workflows/msix.yml` (superseded).
- Reused unchanged: `scripts/publish.cmd`, `scripts/publish-msix.ps1`, `scripts/changelog-section.ps1`.
- Updated: `packaging/SUBMISSION.md`, `AGENTS.md` (release flow and commands).
- External: GitHub repository variables, secrets and an environment with a required reviewer; an Entra ID app registration associated with the Partner Center account; the Microsoft Store Developer CLI (`msstore`) installed on the runner by the workflow. No new application dependencies; the app itself does not change.
