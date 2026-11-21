# Microsoft Store submission checklist

Registration in Partner Center is free (start at <https://storedeveloper.microsoft.com>).

## One-time
- [x] Developer account created and app name `Moth-Eidol` reserved.
- [x] Identity values saved in `packaging/store-identity.local.json` (git-ignored; template in `store-identity.example.json`).
- [ ] Privacy policy published: <https://pasta0126.github.io/Moth-Eidol/privacy> (source: `docs/privacy.md`, GitHub Pages from `main` /docs).

## Per release (automated)
Nothing is published by pushing a commit or a tag; publication is started by hand.
1. Write the `CHANGELOG.md` entry `## [x.y.z] - YYYY-MM-DD` and commit it. `pwsh -File scripts/changelog-section.ps1 -Version x.y.z` must succeed (1500 characters at most).
2. `scripts/release.sh x.y.z` on a clean `main`: checks the version and the entry, sets the version in `Directory.Build.props`, commits `Release x.y.z`, tags `vX.Y.Z` and pushes.
3. Start the **Release** workflow (GitHub > Actions > Release > Run workflow, or `gh workflow run release.yml -f version=x.y.z -f store=true`):
   - `check`: the tag exists, `Directory.Build.props` at the tag is `x.y.z`, the changelog entry is valid.
   - `build`: tests, `MothEidol-x.y.z-win-x64.zip` and the Store `.msix` (kept as run artifacts).
   - `github-release`: public release `vX.Y.Z` with the changelog entry as notes and the zip (the MSIX is not attached: it only installs through the Store). Running it again updates the release.
   - `store` (only with `store` checked): waits for your approval in the run page, then uploads the MSIX as a new submission, sets "What's new in this version" in every listing language from the changelog and submits it for certification.
4. If the `store` step fails, the GitHub Release stays; a draft submission may remain in Partner Center. Finish it by hand (download the `.msix` from the run artifacts) or run the workflow again with `store` checked.

Screenshots, description and the rest of the listing are not touched by the workflow; edit them in Partner Center when needed.

### One-time setup of the automated release
Repository variables (GitHub > Settings > Secrets and variables > Actions > Variables; not secrets, they are visible in the published package):
- `STORE_IDENTITY_NAME`, `STORE_PUBLISHER`, `STORE_PUBLISHER_DISPLAY_NAME`: the values of `packaging/store-identity.local.json` (Partner Center > Moth-Eidol > Product management > Product identity: `Package/Identity/Name`, `Package/Identity/Publisher`, `Package/Properties/PublisherDisplayName`).

Environment `store` (GitHub > Settings > Environments > New environment):
- Required reviewers: the maintainer (this is the approval before anything reaches the Store). Deployment branches and tags: tags `v*`, branch `main`.
- Variable `MSSTORE_PRODUCT_ID`: the Store ID of the app (Partner Center > Moth-Eidol > Product management > Product identity > Store ID, `9...`).
- Secrets, from an Entra ID application associated with the Partner Center account:
  1. Partner Center > Settings (gear) > Account settings > User management > Microsoft Entra applications > Create Microsoft Entra application (or Add an existing one), role **Manager**.
  2. `MSSTORE_TENANT_ID`: the tenant ID shown there (also Account settings > Organization profile > Tenants).
  3. `MSSTORE_CLIENT_ID`: the application (client) ID.
  4. `MSSTORE_CLIENT_SECRET`: Add new key on that application; copy the value at once (it is shown only once).
  5. `MSSTORE_SELLER_ID`: Account settings > Legal info > Developer > Seller ID.
- The client secret expires (the expiry date is shown with the key). Before it does, add a new key in Partner Center and replace `MSSTORE_CLIENT_SECRET`; an expired one makes the `store` step fail at sign-in without submitting anything.

## Per release (manual fallback, Windows host with the Windows SDK)
0. Make sure `CHANGELOG.md` has the entry of the version being submitted, and get the Store text with `powershell -File scripts\changelog-section.ps1 -Version <version>`.
1. `powershell -File scripts\publish-msix.ps1` -> `artifacts\MothEidol-<version>-win-x64.msix` (unsigned; the Store signs it).
2. Without the identity file the script builds `...-test.msix`, signed with a local certificate, for sideload testing only.
3. Install the test package and verify: tray, single instance, local folders and network shares, data location, start with session on/off (also from Windows Settings > Apps > Startup).
4. Run the Windows App Certification Kit against the package and fix findings.
5. Partner Center > Moth-Eidol > Start submission:
   - Pricing and availability (free, markets).
   - Properties: category, privacy policy URL, support contact.
   - Age rating questionnaire.
   - Packages: upload the `.msix`.
   - Store listing: description, "What's new in this version" (the output of step 0), screenshots (at least one, 1366x768 or larger), logos.
   - Notes for certification: explain `runFullTrust` (reads log files on arbitrary local folders and network shares with the user's permissions).
6. Submit and wait for certification (usually 1-3 business days).

## Media needed for the listing
- [ ] Screenshots of the running app: PNG, 1366x768 or larger (up to 3840x2160), at least 1 (recommended 4-8), up to 10, max 50 MB each; optional caption of 200 characters or fewer.
- [x] 1:1 app tile icon, 300x300 PNG: `packaging/store-listing/app-tile-icon-300x300.png` (generated by `packaging/generate-icons.py`).
- Optional (not required for apps): 16:9 super hero art 1920x1080 (no text, no UI), trailer videos.

## Branding and tagline
- Tagline: **Log tracking & monitoring** (used in the Store description, manifest and GitHub Pages).
- Banner artwork: `assets/branding/moth-eidol-banner.png` (1672x941, contains the name and tagline, so it is NOT valid as the Store 16:9 super hero, which must have no text or UI).
- Derived by `packaging/generate-icons.py`: `assets/branding/moth-eidol-social-preview.png` (1280x640, upload it in GitHub > Settings > Social preview) and the MSIX `SplashScreen.png`.
