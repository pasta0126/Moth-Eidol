## 1. Changelog

- [ ] 1.1 Reconstruct `CHANGELOG.md` from `0.1.0` to `1.7.0` using the git tags (versions and dates) and the archived changes (wording), in user-facing language, taking commit `1cc5ebc` as `1.0.0` (it has no tag)
- [ ] 1.2 Have the maintainer review the wording of the entries

## 2. Process

- [ ] 2.1 Update `AGENTS.md`: the release flow becomes archive, changelog entry, bump version, commit, push and tag
- [ ] 2.2 Add the changelog entry to the checklist for finishing a change

## 3. Store text

- [ ] 3.1 Confirm the Store "What's new" length limit in Partner Center
- [ ] 3.2 Add a helper script in `scripts/` that prints a version's changelog entry as plain text and fails when it is missing or too long
- [ ] 3.3 Update `packaging/store-listing/listing-en-US.md` and `packaging/SUBMISSION.md` so each submission includes the "What's new" text from the changelog
- [ ] 3.4 Optional: attach the Store text as an artifact in the "Build MSIX" workflow, and offer the same section as GitHub release notes

## 4. Verification

- [ ] 4.1 Tests or a script check for the extraction (existing version, missing version, too long)
- [ ] 4.2 Build and run the tests
- [ ] 4.3 Generate the "What's new" text for `1.7.0` and check it reads well (deferred to the maintainer)
