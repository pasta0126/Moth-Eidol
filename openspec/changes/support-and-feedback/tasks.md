## 1. Open URL abstraction

- [ ] 1.1 Add a platform-isolated "open URL in the default browser" abstraction with macOS, Windows and Linux implementations, accepting only fixed `https` addresses
- [ ] 1.2 Handle failure to open the browser without crashing (message and copy of the address)

## 2. About block

- [ ] 2.1 Add the About block at the end of `AppSettingsView` with the first-person text and the three plain-text links, using `Moth*Brush` resources and selectable text
- [ ] 2.2 Keep the footer visible and check the block in a narrow window

## 3. Repository and documents

- [ ] 3.1 Add `.github/ISSUE_TEMPLATE/` with the suggestion and bug report templates and `config.yml` (blank issues disabled, Buy me a coffee contact link)
- [ ] 3.2 Update `docs/privacy.md` (links only open the browser; no data sent by the app) with a new "Last updated" date
- [ ] 3.3 Review the Store listing and review notes for the donation link

## 4. Verification

- [ ] 4.1 Unit tests for the URL validation (only fixed `https` addresses)
- [ ] 4.2 Build and run the tests
- [ ] 4.3 Check manually on macOS and Windows that each link opens the right page and the block looks discreet (deferred to the maintainer)
- [ ] 4.4 Check that the issue chooser shows both templates and no blank issue
