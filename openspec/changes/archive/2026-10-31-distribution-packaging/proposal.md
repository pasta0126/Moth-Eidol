## Why

The application can only be run from the sources with the .NET SDK installed. Each team member runs it on their own PC (Windows first, macOS), so there must be a way to produce a package that runs after unzipping: no installer, no administrator rights and no .NET installed beforehand.

## What Changes

- A publish script produces, for each supported platform, a self-contained single-file build (runtime included, native libraries bundled) and zips it as `MothEidol-<version>-<platform>.zip`.
- Platforms: `win-x64` (priority), `osx-arm64`, `osx-x64` and `linux-x64`.
- On macOS the package is a `MothEidol.app` bundle with an icon, ad-hoc signed so it starts on Apple Silicon.
- Each package carries a short `README.txt` with how to run it and how to get past the unsigned-application warning (SmartScreen, Gatekeeper).
- The packages never need elevated rights: nothing is installed, and data stays in the user's profile.

## Capabilities

### New Capabilities
- `distribution`: portable, self-contained packages per platform and how they are produced.

### Modified Capabilities

## Impact

- New `scripts/publish.sh` and `scripts/publish.cmd`, a macOS bundle template (Info.plist, icon) and `artifacts/` (already git-ignored) as output.
- `MothEidol.App.csproj` publish settings; no runtime code changes and no new dependencies.
- Code signing with a real certificate and auto-update are out of scope.
