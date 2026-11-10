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

## Decisions settled with the maintainer

- **Primary color:** deep navy `#202536` (buttons, headings, the default project accent); gold `#C8923E` is only a detail (selection, badges), never text.
- **Semantic colors in Vesper:** info `#2F6F73` (5.1:1), warning `#9A5B00` (4.8:1), error `#B3261E` (5.8:1) on ivory.
- **Traffic light in Vesper:** green `#2E7D32`, yellow `#B8860B`, orange `#C2570C`, red `#B3261E`. They are dots, not text, so they need 3:1 (the yellow is the weakest, 2.9:1 on ivory, and is always paired with its text).
- **Implementation:** Moth is the Dark theme variant and Vesper the Light one, both defined in `MothTheme.axaml` under the same `Moth*Brush` names. Switching the application's theme variant at runtime updates every `DynamicResource` and the Fluent controls (inputs, combo boxes, check boxes) without restarting.
- **Project accent in Vesper:** the 16 swatches were chosen for the dark theme and most are illegible as text on cream (for example 1.2:1 to 2.6:1). The chosen color is kept as is for the tile stripe and frame, but the accent used as *text* (the project name) is shifted toward the text color until it reaches 4.5:1 against the tile background, in either theme. This also covers any hex the user types. The palette is not duplicated per theme.
- **Tray icon:** it does not follow the app theme (it keeps the Moth colors, and a Vesper-aware icon belongs to `tray-icon-theming`). The toast does follow the active theme.
- **Artwork:** the banner and icons are checked in the real theme during manual verification; variants are only made if they look wrong on ivory.
- **Default theme:** Moth.

## Risks / Trade-offs

- Hard-coded colors in existing views will look wrong in Vesper; an audit is needed.
- The semantic level colors need care to stay distinguishable from each other on a light background.
