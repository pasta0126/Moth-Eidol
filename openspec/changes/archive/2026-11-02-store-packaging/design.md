## Context

The app ships today as self-contained zips built by `scripts/publish.sh` (spec `distribution`). Autostart is behind `IAutostart` with a Windows registry `Run` value implementation. See proposal.md for motivation. The Store requires a package identity and publisher that only exist once a Partner Center account is registered, so part of the work is blocked until then.

## Goals / Non-Goals

**Goals:**
- A Store-submittable MSIX for win-x64 reusing the existing publish output.
- Same behavior as the zip build (file and share access, data in the user profile, tray, single instance).
- Autostart that works when packaged.

**Non-Goals:**
- arm64 Windows package (can follow later).
- CI automation, auto-update logic of our own (the Store updates the app) and a purchase or licensing model.
- Replacing the zip distribution, which stays.

## Decisions

- **MSIX with `runFullTrust` over unpackaged EXE/MSI submission.** The Store signs the package, so no code-signing certificate is bought; the alternative requires a hosted installer signed by a trusted CA.
- **Build with `makeappx`/`signtool` from the Windows SDK in a script, not a Windows Application Packaging Project.** Keeps the repo cross-platform friendly and the packaging a thin step over `publish`. Alternative: a `.wapproj`, which needs Visual Studio tooling.
- **Four-part version `x.y.z.0` derived from `Directory.Build.props`.** The Store reserves the last part, so the single source of version stays intact.
- **Packaged autostart via the `StartupTask` API, selected at runtime.** Detect packaged execution and use a startup-task implementation of `IAutostart`; otherwise keep the registry one. The manifest declares the `windows.startupTask` extension. The registry value must not be used when packaged, because the OS startup settings would not reflect it.
- **Identity and publisher injected by the script from parameters or a git-ignored file.** The real values are never hard-coded; without them only a test-signed package is produced.
- **Logo assets generated from the moth artwork** into the required sizes and committed under a packaging folder.

## Risks / Trade-offs

- [Certification rejection for full trust] → Justify `runFullTrust` in the submission notes: reading logs on arbitrary folders and shares requires it.
- [AppData redirection of packaged writes] → Data stays inside the package-private store; verify it survives updates and document where it lives.
- [Startup task behaves differently from the registry value] → Cover with a manual test on a Windows machine, since it cannot run in unit tests; keep the abstraction testable with a fake.
- [Packaging needs a Windows host] → Document it; the zip flow is unaffected.

## Open Questions

- Partner Center account type (individual or company) and final reserved name.
- Privacy policy hosting location.
