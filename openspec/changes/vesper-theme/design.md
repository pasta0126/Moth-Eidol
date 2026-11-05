## Context

There is one dark theme in `src/MothEidol.App/Themes/MothTheme.axaml`, derived from the moth artwork (obsidian, amber, copper, cream, one turquoise accent). Views are required to use the `Moth*Brush` resources through `DynamicResource`, which makes a runtime theme switch feasible. The Vesper palette comes from the maintainer's Vesper Zero project style guide (warm ivory, deep navy, soft gold). Details are intentionally left open until development.

## Goals / Non-Goals

**Goals:**
- Offer Vesper as a second, fully readable theme next to Moth.
- Switch themes at runtime from the application settings and remember the choice.

**Non-Goals:**
- A generic light theme (to be analysed separately later), a "follow the system" option, user-defined or editable themes, per-project themes.
- Copying the web style of Vesper Zero (large radii, soft shadows, decorative curves): only the palette is taken.

## Decisions

- Keep the same brush names in both themes so no view needs to know which theme is active. Rationale: the dark theme already follows this rule.
- Store the choice as an application setting alongside the existing ones.
- **Vesper base palette** (from the style guide) and its mapping to the existing resources:

  | Resource | Moth (dark) | Vesper |
  |---|---|---|
  | `MothBg` | `#1D1511` | `#F5F0E7` ivory |
  | `MothSurface` | `#302017` | `#FBF8F2` cream |
  | `MothSurfaceAlt` | `#5B2D11` | `#EDE5D9` beige |
  | `MothBorder` | `#724727` | `#DED5C8` |
  | `MothText` | `#FBF2DC` | `#22242B` |
  | `MothTextMuted` | `#B08C64` | `#65666D` |
  | `MothPrimary` | `#DF651D` | `#202536` navy (see open questions) |
  | `MothAccent` | `#FCC86E` | `#C8923E` gold |

  Other palette values available: deep navy `#151927`, secondary blue-gray `#3D4355`, light gold `#DCAF65`, pale gold `#E8D3A8` (soft backgrounds and badges).
- **Contrast:** primary text (13.6:1) and muted text (5.0:1) pass on ivory. Gold as text (2.4:1) and the palette's faint text `#96928A` (2.7:1) do not, so gold is used as a detail (selection, badges), not for readable text, and the faint text color is not used for content.
- **Semantic colors:** the Moth warning, info and error colors are too light on cream (about 2.1:1, 2.8:1 and 4.2:1), so Vesper gets deeper variants that keep their character (amber, teal, red), possibly with a pale gold background for warning rows. Exact values to be defined with a visual preview.

## Open questions

- Is the primary color navy (buttons and headings) with gold only as accent, or gold as primary? The proposal leans to navy primary and gold accent.
- Exact values of the Vesper semantic colors (warning, info, error) and the selection highlight.
- How are the tray icon and the toast window themed? They may follow the OS rather than the app theme (related to the `tray-icon-theming` backlog item).
- Do the artwork assets (banner, icons) look right on ivory, or are Vesper variants needed?
- Which theme is the default for new installs (Moth, as today)?

## Risks / Trade-offs

- Hard-coded colors in existing views will look wrong in Vesper; an audit is needed.
- The semantic level colors need care to stay distinguishable from each other on a light background.
