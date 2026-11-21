## 1. Release preparation script

- [x] 1.1 Add `scripts/release.sh <x.y.z>` with all checks before any change: format, greater than the current `x.y.z` (numeric per component), clean tree, branch `main`, tag absent locally and on `origin`, dated changelog heading
- [x] 1.2 After the checks: rewrite `<Version>` in `Directory.Build.props`, commit `Release x.y.z`, annotated tag `vX.Y.Z`, `git push --atomic origin main vX.Y.Z`
- [x] 1.3 Try each refusal case in a scratch clone (bad format, not greater, dirty tree, other branch, existing tag, missing entry) and one successful run against a local bare remote; confirm nothing changes on refusal

## 2. Release workflow

- [x] 2.1 Add `.github/workflows/release.yml` with `workflow_dispatch` inputs `version` and `store` (default false), and no push or tag trigger
- [x] 2.2 `check` job: checkout `refs/tags/vX.Y.Z` (fail if missing), compare the tagged `Directory.Build.props` with the input, run `scripts/changelog-section.ps1 -Version x.y.z`, extract the markdown section for the release notes, upload both as artifacts
- [x] 2.3 `build` job: test in Release, `scripts\publish.cmd`, write `packaging/store-identity.local.json` from the variables `STORE_IDENTITY_NAME`, `STORE_PUBLISHER`, `STORE_PUBLISHER_DISPLAY_NAME` (fail naming the empty one), `scripts\publish-msix.ps1`, upload the zip and the MSIX
- [x] 2.4 `github-release` job: create `vX.Y.Z` with the markdown notes and the zip, or update notes and replace the zip with `--clobber` when the release exists; never attach the MSIX
- [x] 2.5 `store` job: `if: inputs.store`, `environment: store`; check the four `MSSTORE_*` secrets and `MSSTORE_PRODUCT_ID` (fail naming the missing one), install `msstore` with `microsoft/microsoft-store-apppublisher`, `reconfigure`, `publish --noCommit`, set `ReleaseNotes` of every listing language with `submission get`/`update`, `submission publish`; on failure, a run summary says the Store was not updated
- [x] 2.6 Make sure no secret is echoed: no tracing of commands with credentials, credentials passed only through environment variables of the `store` job
- [x] 2.7 Delete `.github/workflows/msix.yml`

## 3. Documentation

- [x] 3.1 Update `packaging/SUBMISSION.md`: automated release order (changelog, `release.sh`, Release workflow, approval) and one-time setup (repository variables, `store` environment with required reviewer, Entra ID app associated in Partner Center with Manager role, secrets and where each value comes from, product ID, client secret renewal); keep the manual steps as fallback
- [x] 3.2 Update `AGENTS.md`: release flow step 4 uses `scripts/release.sh`, publication only via the Release workflow, add the commands to the Commands section and remove the `Build MSIX` mention

## 4. Verification

- [x] 4.1 With the maintainer, create the repository variables and the `store` environment
- [ ] 4.2 Run the Release workflow for a version that is not tagged and confirm it stops in `check`; run it with `store: false` for the next real version and check the GitHub Release (notes, zip, no MSIX) and a second run that updates it
- [ ] 4.3 With the maintainer, set up the Entra ID app and secrets, run with `store: true`, approve, and confirm the submission in Partner Center has the new package and the changelog text; record any difference in the `msstore` commands in the design
