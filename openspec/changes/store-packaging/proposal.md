## Why

Windows users expect to find and update applications through the Microsoft Store, which also removes the SmartScreen "unknown publisher" warning that the unsigned zip triggers today. Publishing there widens distribution without requiring a paid code-signing certificate, since the Store signs the package.

## What Changes

- Add an MSIX package for Windows x64, built from the existing self-contained `win-x64` publish output, with a manifest that declares full trust so log folders and network shares remain readable with the OS user's permissions.
- Add store logo assets derived from the moth artwork.
- Add a script that builds the MSIX, plus local test signing for sideload testing (the Store signs the final package).
- Start with the session through the MSIX startup task when the app runs packaged; the registry value is kept for the zip build.
- Derive the four-part MSIX version `x.y.z.0` from `Directory.Build.props`.
- Partner Center registration is free for individual and company accounts (since 2025/2026), so there is no registration cost.
- Document the Partner Center submission checklist (listing, screenshots, age rating, privacy policy URL, certification).
- Packaged identity (`Identity Name`, `Publisher`) comes from Partner Center and is pending until the account exists.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `distribution`: adds the MSIX package for the Microsoft Store alongside the existing zips.
- `app-shell`: start with session must work when the app runs as an MSIX package.

## Impact

- New `scripts/publish-msix` script, package manifest and assets; Windows host (Windows SDK `makeappx`) required to build.
- `src/MothEidol.Core/Platform`: a packaged-aware autostart implementation behind `IAutostart`.
- No new runtime dependencies expected; the Windows SDK is a build-time tool only.
- Backlog item: not scheduled, and no version bump until implemented.
