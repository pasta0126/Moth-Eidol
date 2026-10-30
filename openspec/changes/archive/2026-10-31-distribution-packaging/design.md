## Context

The app is `net10.0` Avalonia with SQLite (native library) and Skia (native). Nothing in the repo publishes it today. Data already lives under the user profile (`DataDirectory`), autostart uses `HKCU`, so no elevation is needed.

## Goals / Non-Goals

**Goals:** a zip per platform that runs after unzipping; reproducible from one script; works from the development Mac for all targets.

**Non-Goals:** installers (MSI/DMG), real code signing and notarization, auto-update, CI release automation, ARM Windows/Linux.

## Decisions

- **`dotnet publish` with `SelfContained`, `PublishSingleFile`, `IncludeNativeLibrariesForSelfExtract` and compression.** One file per platform is easiest to hand out. Native libraries extract to a per-user temp folder on first start, which needs no rights. Trimming is **off**: Avalonia compiled bindings and reflection make it risky and the size gain is not worth a broken UI.
- **Cross-publish from macOS.** The app is managed code, so every RID can be published from one machine; runtime packs are restored from NuGet.
- **One bash script plus a `.cmd` wrapper for Windows.** The bash script holds the logic (bundle creation needs macOS tools anyway); the `.cmd` covers Windows publishing of the Windows/Linux targets via `dotnet publish` and `tar`/PowerShell zip.
- **macOS `.app` bundle** built by the script: `Contents/MacOS/<executable>`, `Info.plist` from a template with version substituted, `Contents/Resources/MothEidol.icns` generated with `iconutil` from `app-icon.png`, then `codesign --force --deep -s -`. When built on a non-macOS host the bundle is still assembled but unsigned, and the README says so.
- **Output** in `artifacts/` (already git-ignored): `artifacts/publish/<rid>/` and `artifacts/MothEidol-<version>-<rid>.zip`.
- **README** is generated per platform from a template in `scripts/dist/`.

## Risks / Trade-offs

- Unsigned binaries trigger SmartScreen/Gatekeeper warnings → documented in the README; signing is a later change.
- Single-file self-extraction needs a writable temp folder → standard on user sessions.
- Larger downloads (runtime included, roughly 60-80 MB compressed) → accepted for zero-install.
- Only the host platform can be run to verify; the other zips are verified structurally.
