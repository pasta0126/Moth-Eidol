## 1. Model and persistence

- [ ] 1.1 `Project` and alert settings model with validation
- [ ] 1.2 Migration: projects table, project reference on sources, "Unassigned" project for existing sources
- [ ] 1.3 Project repository on `local-persistence`
- [ ] 1.4 Project service: create, rename, edit, enable, remove, with hot reload of running sources
- [ ] 1.5 Project status derived from source health

## 2. Dashboard

- [ ] 2.1 Dashboard as the home screen with a tile per project and the "New project" tile
- [ ] 2.2 Tile status visuals using `Moth*Brush` resources (healthy, degraded, not configured, disabled)
- [ ] 2.3 Create a project from the dashboard (name only) and open its screen
- [ ] 2.4 Navigation: tile opens the project, back to the dashboard, application settings reachable

## 3. Project screen

- [ ] 3.1 Project screen shell with Tail and Settings sections and the persistent version footer
- [ ] 3.2 Tail scoped to the project's sources with an all-sources/source filter, reusing the viewer
- [ ] 3.3 Settings: name, enable, sources (file or folder + pattern), warning threshold, frequency mode, remove project
- [ ] 3.4 Remove the global Live and Sources tabs

## 4. Verification

- [ ] 4.1 Tests: validation, persistence, migration of existing sources, hot reload, removal stops sources, status derivation
- [ ] 4.2 Run on macOS with the demo projects (generator per project) and check tiles, navigation, tail and settings
- [ ] 4.3 Close the change: archive, bump the version (semver), commit, push and tag it
