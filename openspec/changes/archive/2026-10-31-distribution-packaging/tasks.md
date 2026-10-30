## 1. Publish settings and script

- [x] 1.1 Add `scripts/publish.sh` that publishes `win-x64`, `osx-arm64`, `osx-x64` and `linux-x64` as self-contained single files and zips them as `MothEidol-<version>-<rid>.zip`; reject unknown platforms
- [x] 1.2 Add `scripts/publish.cmd` for Windows hosts
- [x] 1.3 Add the per-platform README template and include it in every zip

## 2. macOS bundle

- [x] 2.1 Add the Info.plist template and generate the `.icns` from `app-icon.png`
- [x] 2.2 Assemble `MothEidol.app` and ad-hoc sign it when run on macOS

## 3. Verification

- [x] 3.1 Publish all platforms; check each zip's contents
- [x] 3.2 Run the published macOS build without the .NET SDK on the path and confirm it starts and creates its data in the user profile
- [x] 3.3 `dotnet test` and `openspec validate --all` pass
- [x] 3.4 Document the commands in AGENTS.md; archive, bump the version, commit and tag
