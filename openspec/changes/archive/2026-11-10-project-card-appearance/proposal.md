## Why

Dashboard tiles are hard to read and impossible to personalize: the status is colored inconsistently, the detail line ("Nothing pending · 1 source") distracts instead of informing, projects are always sorted by name, and every tile looks the same. People who watch many projects need to recognize them quickly and read their state at a glance.

## What Changes

- Each project gets an optional **accent color**, chosen in its Settings from a 16-color palette or typed as a hex value; when unset, the default accent is used. The tile uses it as its accent.
- The tile status uses **traffic-light colors** (green, yellow, orange, red; neutral when not monitored) shown as a colored dot followed by the status text.
- The detail line is removed from healthy tiles (no more "Nothing pending · N sources"); other detail text is reviewed so every message on a tile is accurate and actionable.
- Tiles can be **reordered manually by drag and drop**; the order is stored and kept across restarts. New projects go last.
- **BREAKING**: tiles are no longer ordered by name by default; existing projects keep their current (name) order until the user moves one.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `project-dashboard`: tile status colors and dot, removal of the healthy detail line, manual order, accent on the tile.
- `projects`: project accent color and its settings editor, stored position.

## Impact

- Core: `Project` gains accent color and position; SQLite migration; repository/service ordering.
- App: `TileModel`, `DashboardView`, `ProjectSettingsView` (color picker with hex field), theme brushes for the traffic light.
- Specs: deltas for `project-dashboard` and `projects`. No new dependencies.
