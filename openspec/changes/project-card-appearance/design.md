## Context

See proposal.md - Why. `TileModel.For` builds status text, brush and detail; `Project` is `(Id, Name, Enabled, Alerts)` persisted in SQLite; the dashboard sorts by name.

## Goals / Non-Goals

**Goals:** per-project accent, traffic-light status, cleaner tile text, persisted manual order.
**Non-Goals:** per-theme color variants, user-defined palettes, reordering inside the project screen, one color for the card background and another for the accent (a single color is used).

## Decisions

- **Traffic-light mapping** (assumption, easy to change): green = healthy with nothing pending; yellow = warnings pending; orange = degraded (source unavailable); red = errors pending; neutral grey = not configured or disabled. Worst state wins (red > orange > yellow > green). Defined as `Moth*` brushes in `MothTheme.axaml`, never hard-coded in views.
- **Accent storage:** nullable `AccentColor` text (`#RRGGBB`) on the project row; null means default. Validation lives in Core (parse/normalize, accepts with or without `#`, 3 or 6 digits). Invalid hex is rejected in the editor, not stored.
- **Palette:** 16 fixed swatches chosen to stay legible on the obsidian background and distinct from the traffic-light hues; a "default" swatch clears the value.
- **Order:** integer `Position` column, migration seeds it from the current name order; new projects get max+1. Drag and drop uses Avalonia pointer events on the tile (no new dependency) and rewrites positions in one transaction.
- **Detail line:** shown only when it carries information the status does not (unavailable source, not configured hint, disabled). Healthy tiles show only dot + status + trend.

## Risks / Trade-offs

- [Yellow/orange confusable] → pair every dot with its text; keep hues well separated.
- [Drag and drop vs. click to open] → start the drag only after a movement threshold; a plain click still opens the project.
- [User accent resembles a status color] → the accent is shown on the tile frame/name only; the status dot keeps the traffic-light colors.

## Migration Plan

Add `AccentColor` (null) and `Position` columns with a migration; seed `Position` by name. Rollback not needed (additive).
