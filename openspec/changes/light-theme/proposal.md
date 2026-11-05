## Why

The application has a single dark theme. Some people work in bright environments or simply prefer light interfaces, and a light theme is a common expectation for a desktop tool that is left open all day. This is a future item: the proposal is deliberately light and the details are to be defined when development starts.

## What Changes

- A light theme is added next to the existing dark `MothTheme`, using the same `Moth*Brush` resource names so views keep working unchanged.
- The user chooses the theme in the application settings; the choice is persisted and applied without restarting.
- Semantic level colors (info, warning, error) stay recognizable and readable in both themes.
- Open: whether a "follow the system" option exists (see the design).

## Capabilities

### New Capabilities
- `app-theme`: the available visual themes, how the user chooses one and how it is applied and persisted.

### Modified Capabilities

None. The "single dark theme" statement in `AGENTS.md` (UI theme section) will be updated when the change is implemented.

## Impact

- `src/MothEidol.App/Themes/` (a second theme dictionary), `App.axaml`, application settings view and the settings stored in SQLite (one new setting).
- Any view that hard-codes a color instead of using a `Moth*Brush` resource must be fixed.
- No new dependencies.
