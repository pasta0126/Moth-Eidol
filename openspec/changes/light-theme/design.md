## Context

There is one dark theme in `src/MothEidol.App/Themes/MothTheme.axaml`, derived from the moth artwork (obsidian, amber, copper, cream, one turquoise accent). Views are required to use the `Moth*Brush` resources through `DynamicResource`, which makes a runtime theme switch feasible. Theme details are intentionally left open until development.

## Goals / Non-Goals

**Goals:**
- Offer a light theme that keeps the identity of the moth palette (amber as the primary color).
- Switch themes at runtime from the application settings and remember the choice.

**Non-Goals:**
- User-defined or editable themes.
- Per-project themes.

## Decisions

- Keep the same brush names in both themes so no view needs to know which theme is active. Rationale: the dark theme already follows this rule.
- Store the choice as an application setting alongside the existing ones.

## Open questions

- Should there be a "Follow system" option (and should it be the default), or only Dark / Light?
- Which exact palette does the light theme use (cream background with obsidian text, amber kept as primary)? To be defined with the maintainer, ideally with a visual preview.
- Do the artwork assets (banner, icons) need light variants?
- How are the tray icon and the toast window themed (they may follow the OS rather than the app theme; related to the `tray-icon-theming` backlog item)?

## Risks / Trade-offs

- Hard-coded colors in existing views will look wrong in the light theme; an audit is needed.
- Contrast of the warning and error colors on a light background must be checked for readability.
