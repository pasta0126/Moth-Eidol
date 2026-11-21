## Context

The release flow (AGENTS.md) is: changelog entry, version bump dropping the development fourth part, commit, tag `vX.Y.Z`, push, maintainer verification, and a manual Store submission from a Windows host or the `Build MSIX` workflow (`workflow_dispatch` with the identity typed as inputs). The building blocks exist and work: `scripts/publish.cmd` (win-x64 zip), `scripts/publish-msix.ps1` (MSIX, unsigned when `packaging/store-identity.local.json` exists, test-signed otherwise; refuses a four-part version), `scripts/changelog-section.ps1` (Store text, fails when missing or over 1500 characters). There is no CI and no GitHub Release. Development happens on macOS; only Windows artifacts are in scope.

## Goals / Non-Goals

**Goals:**
- One command to cut a release locally, with the same checks every time.
- One manual trigger to publish a tagged version: GitHub Release with the zip and, after approval, Store submission.
- Reuse the existing scripts so local and CI builds are identical.

**Non-Goals:**
- macOS/Linux artifacts, signing or notarization.
- Publishing on tag push or on merge.
- Automating screenshots, listing text other than "What's new", pricing or age rating.
- Automating the development `a` bump.
- A build-and-test run on every push (possible later as its own workflow).

## Decisions

### Two separate pieces: `scripts/release.sh` and `release.yml`
Tagging (done at the end of every change, also for versions never submitted) and publishing stay separate decisions, as the changelog rule already assumes ("a version that is tagged but not submitted is folded into the next one"). `release.yml` is `workflow_dispatch` only, with inputs `version` (string, `x.y.z`) and `store` (boolean, default `false`). Alternative rejected: trigger on `v*` tags, because it would publish every tagged version.

### `release.sh` in bash
The maintainer cuts releases on macOS, where the other dev scripts (`run.sh`, `publish.sh`) are bash. It checks and only then mutates: format, greater than the current `x.y.z` (compared numerically per component, so `1.10.0` > `1.9.0`), clean tree, branch `main`, tag absent locally and on `origin`, and a heading matching `^## \[x\.y\.z\] - \d{4}-\d{2}-\d{2}`. It then rewrites `<Version>` with `sed`, commits `Release x.y.z`, creates an annotated tag and runs `git push --atomic origin main vX.Y.Z`. No Windows equivalent for now: a Windows maintainer can run it from Git Bash.

### Release workflow jobs
1. `check` (`windows-latest`): checkout the tag `vX.Y.Z` (fails if missing), read `Directory.Build.props` and compare it with the input, run `changelog-section.ps1 -Version x.y.z` and save its output as a file.
2. `build` (needs `check`): setup .NET 10, `dotnet test -c Release`, `scripts\publish.cmd`, write the identity file from repository **variables** `STORE_IDENTITY_NAME`, `STORE_PUBLISHER`, `STORE_PUBLISHER_DISPLAY_NAME` (failing with the missing name if empty), then run `scripts\publish-msix.ps1`. Upload zip, MSIX and notes as run artifacts. Variables, not secrets, because these values are public in the published package and keeping them visible makes mistakes easy to spot.
3. `github-release` (needs `build`, `ubuntu-latest`, `contents: write`): `gh release create vX.Y.Z` with `--notes-file` and the zip, or `gh release edit` + `gh release upload --clobber` when it already exists. Notes use the markdown section of the changelog (same lines as the Store text, but kept as markdown), extracted with a small `awk` step so links render on GitHub.
4. `store` (needs `github-release`, `if: inputs.store`, `environment: store`): the `store` environment has the maintainer as required reviewer and holds the secrets, so the approval prompt appears before any credential is exposed to the job.

Only the zip goes into the GitHub Release: the Store MSIX is unsigned and cannot be installed by users.

### Store submission with the Microsoft Store Developer CLI
`msstore` is Microsoft's supported CLI for Partner Center (it wraps the Submission API), installed by the official action `microsoft/microsoft-store-apppublisher`, so no extra code in the repo. Updates through it are supported for free apps that already have a published submission, which is the case. Credentials: Entra ID app associated with the Partner Center account (Account settings > User management > Microsoft Entra applications, role Manager), stored as environment secrets `MSSTORE_TENANT_ID`, `MSSTORE_CLIENT_ID`, `MSSTORE_CLIENT_SECRET`, `MSSTORE_SELLER_ID`; the Store product ID as environment variable `MSSTORE_PRODUCT_ID`. Sequence (documented for MSIX apps):
1. `msstore reconfigure --tenantId --sellerId --clientId --clientSecret`.
2. `msstore publish <dir> --inputFile <msix> --appId <productId> --noCommit`: deletes any pending draft, creates a new one from the last published submission and uploads the package, without submitting. It must run before editing metadata, because it replaces the draft.
3. `msstore submission get <productId>` → JSON; set `Listings.<lang>.BaseListing.ReleaseNotes` to the Store text for every language; `msstore submission update <productId> <json>`.
4. `msstore submission publish <productId>`, which sends it to certification.
Telemetry is turned off with `msstore settings --enableTelemetry false`. The CLI is in preview: if a command differs, the step fails before `submission publish` and leaves only a draft, which the maintainer can finish by hand.

Alternative rejected: driving Partner Center from a browser, or a third-party action with access to the credentials.

### Replace `msix.yml`
Its job is covered by `release.yml`, which builds the MSIX from a tag with the identity from variables. Keeping both would mean two ways to build the Store package. A test-signed sideload package is still available locally from `publish-msix.ps1` without the identity file.

## Risks / Trade-offs

- [Store API changes or `msstore` behavior differs from the docs] → the Store step is last and optional, the GitHub Release is already out, and the manual checklist remains the fallback.
- [A pending draft submission already exists in Partner Center] → `msstore publish` replaces it with a new draft; a submission already in certification makes the step fail with the CLI's message, and the maintainer waits or cancels it and runs the workflow again with `store` on (the release job is idempotent).
- [Client secret expires (Entra ID secrets have a maximum lifetime)] → documented in SUBMISSION.md with the renewal steps; the step fails with an authentication error, nothing partial is submitted.
- [Public zip is unsigned and triggers SmartScreen] → already covered by the README in the zip (distribution spec); signing stays out of scope.
- [Notes markdown and Store text drift] → both come from the same changelog section at the tag.

## Migration Plan

1. Create the repository variables and the `store` environment; delete `msix.yml`.
2. Do a dry run with `store: false` for the next version and check the GitHub Release.
3. Set up the Entra ID app and secrets, then run with `store: true` on a real submission.
Rollback: remove the workflows; the manual checklist still works unchanged.

## Open Questions

- The `msstore` sequence comes from Microsoft's documentation; it is confirmed on the first real submission.
- Whether the Store listing has more than one language; the step updates every listing language with the English text.
