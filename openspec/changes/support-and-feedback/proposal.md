## Why

People who use Moth-Eidol have no way to send suggestions or bug reports, and no way to say thanks. The maintainer wants a public, transparent channel that does not look like the app collects data, plus a discreet way to support the project.

## What Changes

- The application settings screen gets a small "About" block at the end, written in the first person, with three plain-text links: Buy me a coffee, Suggestions & bugs, GitHub.
- The links open in the default browser through a small platform-isolated "open URL" abstraction. The app sends nothing by itself.
- Suggestions and bug reports go to the project's public GitHub Issues, through two issue templates (suggestion, bug report) and a template chooser configuration that disables blank issues and links to Buy me a coffee.
- The privacy policy states that the links only open the browser and that the app sends no data on its own.

## Capabilities

### New Capabilities
- `support-links`: the About block, the support and feedback links, how they open, and the issue templates that receive feedback.

### Modified Capabilities

None. The privacy policy (`docs/privacy.md`) is a document, not a spec.

## Impact

- UI: `AppSettingsView` (new block at the end).
- New platform abstraction to open a URL in the default browser, with macOS, Windows and Linux implementations (first time the app opens a web page).
- Repository: `.github/ISSUE_TEMPLATE/` (two templates and `config.yml`), `docs/privacy.md`.
- No storage changes and no new dependencies. The Store listing and review notes may mention the donation link.
