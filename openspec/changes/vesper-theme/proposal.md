## Why

The application has a single dark theme (Moth). The maintainer likes the warm, calm palette of their Vesper Zero project (ivory and cream backgrounds, deep navy, soft gold) and wants it as a second look for Moth-Eidol, so people can choose the one they prefer. A neutral generic light theme is a separate topic and is left for a later analysis. This is a future item: the proposal is deliberately light and details are to be defined when development starts.

## What Changes

- A second theme, **Vesper**, is added next to the existing dark **Moth** theme. Both use the same `Moth*Brush` resource names, so views keep working unchanged.
- The user chooses between Moth and Vesper in the application settings; the choice is persisted and applied without restarting.
- Semantic level colors (info, warning, error) get variants that stay readable on the Vesper cream backgrounds.
- Out of scope: a generic light theme, a "follow the system" option, and user-defined themes.

## Capabilities

### New Capabilities
- `app-theme`: the available themes (Moth and Vesper), how the user chooses one and how it is applied and persisted.

### Modified Capabilities

None. The "single dark theme" statement in `AGENTS.md` (UI theme section) will be updated when the change is implemented.

## Impact

- `src/MothEidol.App/Themes/` (a second theme dictionary), `App.axaml`, the application settings view and the settings stored in SQLite (one new setting).
- Any view that hard-codes a color instead of using a `Moth*Brush` resource must be fixed.
- No new dependencies.
