## 1. Prerequisites (blocked on the maintainer)

- [ ] 1.1 Register the Partner Center account and reserve the app name
- [ ] 1.2 Record the package identity name and publisher values outside the repo and decide where the privacy policy URL is hosted

## 2. Package definition

- [ ] 2.1 Generate the store logo assets from the moth artwork and commit them under a packaging folder
- [ ] 2.2 Add the package manifest template with `runFullTrust` and the `windows.startupTask` extension, with placeholders for identity and publisher
- [ ] 2.3 Derive the four-part version from `Directory.Build.props`

## 3. Build script

- [ ] 3.1 Add `scripts/publish-msix` that stages the `win-x64` publish output with the manifest and assets
- [ ] 3.2 Build the `.msix` with `makeappx` and fail clearly when the SDK tools are missing
- [ ] 3.3 Sign a test package with a local certificate when no store identity is provided, and state that it cannot be submitted

## 4. Autostart when packaged

- [ ] 4.1 Add a startup-task implementation of `IAutostart` and select it when the app runs packaged
- [ ] 4.2 Show the option as unavailable with a reason when the OS blocks the startup task
- [ ] 4.3 Add unit tests for the selection logic using a fake

## 5. Validation

- [ ] 5.1 Install the test package on Windows and verify tray, single instance, data location, local folders and network shares
- [ ] 5.2 Verify autostart on and off, including disabling it from the OS startup apps settings
- [ ] 5.3 Run the Windows App Certification Kit and fix findings

## 6. Submission and docs

- [ ] 6.1 Document the Partner Center submission checklist (listing, screenshots, age rating, privacy policy URL, certification)
- [ ] 6.2 Update `AGENTS.md` and the README for the new package, then archive the change and release
