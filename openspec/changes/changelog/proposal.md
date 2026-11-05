## Why

Users should be able to see what improved between versions. Today the history of each release only lives in commits and archived specs, there is no `CHANGELOG`, and the Microsoft Store "What's new in this version" field is left blank. Keeping a changelog up to date, and reusing it for each Store submission, makes every release explain itself.

## What Changes

- A `CHANGELOG.md` at the repository root, in the "Keep a Changelog" style (Added / Changed / Fixed per version), written in user-facing language, listing only the versions published to the Microsoft Store: `1.0.0` (the first one), `1.3.1` and the next one to submit, `1.7.0`. Versions in between (and the development versions `0.1.0` to `0.10.0`) are folded into the next published entry, and fixes to bugs that never reached a published version are not listed. Reconstructed from the git tags and the archived changes.
- A process rule: when a change is finished, its entry is added to the `CHANGELOG.md` before the version is bumped (folded into the entry of the version that will be submitted to the Store). The release flow becomes archive, changelog, bump version, commit, push and tag.
- The Store "What's new in this version" text for each submission is derived from the changelog entry of the version being submitted, with a small helper script that extracts that section and checks the Store length limit; the submission checklist includes it.
- Out of scope: showing the changelog inside the application.
- Optional: the same section as GitHub release notes.

## Capabilities

### New Capabilities
- `changelog`: the changelog file, its format, and the rule that every released version has an entry.

### Modified Capabilities
- `distribution`: the Store submission checklist also requires the "What's new in this version" text taken from the changelog.

## Impact

- New `CHANGELOG.md` and a helper script in `scripts/`.
- `AGENTS.md` (versioning and release flow), `packaging/store-listing/listing-en-US.md`, `packaging/SUBMISSION.md`; optionally the "Build MSIX" workflow (attach the Store text as an artifact).
- No application code, no storage changes and no new dependencies.
