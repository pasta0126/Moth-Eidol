## 1. Core and persistence

- [ ] 1.1 Add `AccentColor` (nullable) and `Position` to `Project`; migration seeding `Position` by name
- [ ] 1.2 Hex parsing/normalization helper in Core with unit tests (3/6 digits, with/without `#`, invalid)
- [ ] 1.3 Repository/service: list by position, new project last, reorder in one transaction; tests

## 2. Tile status

- [ ] 2.1 Add traffic-light brushes (green, yellow, orange, red) to `MothTheme.axaml`
- [ ] 2.2 Rework `TileModel.For`: mapping with worst-wins, dot + text, drop "Nothing pending · N sources" and the source count; tests for each state
- [ ] 2.3 Review all tile and events-header messages (`TileModel`, `EventsView`) for accuracy and clarity; fix or remove

## 3. Dashboard UI

- [ ] 3.1 Dot + status text in the tile; apply accent to frame and name
- [ ] 3.2 Drag-and-drop reordering with drag threshold; click still opens the project

## 4. Project settings

- [ ] 4.1 Color editor: 16 swatches, hex field with validation message, default option
- [ ] 4.2 Persist and refresh the tile on save

## 5. Wrap-up

- [ ] 5.1 Verify with `dotnet build` and `dotnet test`; manual check on the dashboard
