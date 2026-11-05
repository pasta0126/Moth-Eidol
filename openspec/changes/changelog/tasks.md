## 1. Changelog

- [x] 1.1 Reconstruct `CHANGELOG.md` for the Store-published versions `1.0.0`, `1.3.1` and `1.7.0` (intermediate and `0.x` development versions folded into the next published entry) using the git tags (versions and dates) and the archived changes (wording), in user-facing language, taking commit `1cc5ebc` (tag `v1.0.0`) as `1.0.0`
- [ ] 1.2 Have the maintainer review the wording of the entries

## 2. Process

- [x] 2.1 Update `AGENTS.md`: the release flow becomes archive, changelog entry, bump version, commit, push and tag
- [x] 2.2 Add the changelog entry to the checklist for finishing a change

## 3. Store text

- [ ] 3.1 Confirm the Store "What's new" length limit in Partner Center (deferred to the maintainer: needs Partner Center; 1500 is assumed)
- [x] 3.2 Add a helper script in `scripts/` that prints a version's changelog entry as plain text and fails when it is missing or too long
- [x] 3.3 Update `packaging/store-listing/listing-en-US.md` and `packaging/SUBMISSION.md` so each submission includes the "What's new" text from the changelog
- [ ] 3.4 Optional: attach the Store text as an artifact in the "Build MSIX" workflow, and offer the same section as GitHub release notes

## 4. Verification

- [x] 4.1 Tests or a script check for the extraction (existing version, missing version, too long)
- [x] 4.2 Build and run the tests
- [ ] 4.3 Generate the "What's new" text for `1.7.0` and check it reads well (deferred to the maintainer)
