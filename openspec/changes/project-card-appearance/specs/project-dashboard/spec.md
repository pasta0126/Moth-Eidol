## MODIFIED Requirements

### Requirement: Dashboard as home
The system SHALL open on a dashboard showing one tile per project, in the order the user arranged (by name until the user moves a tile; new projects last), and a tile to create a new project.

#### Scenario: Application start
- **WHEN** the application opens
- **THEN** the dashboard is shown with a tile for every project in the stored order

#### Scenario: No projects yet
- **WHEN** there are no projects
- **THEN** the dashboard shows only the new-project tile and a short hint

### Requirement: Tile status
The system SHALL show on each tile the project's name and its status as a colored dot followed by the status text, using traffic-light colors: green (healthy, nothing pending), yellow (warnings pending), orange (degraded: a source is unavailable or has a read error), red (errors pending) and neutral (not configured or disabled). When several apply, the most severe one SHALL be shown (red over orange over yellow over green).

#### Scenario: Healthy project
- **WHEN** all sources of an enabled project are active and nothing is pending
- **THEN** its tile shows a green dot with "Healthy" and no detail line

#### Scenario: Source down
- **WHEN** a source of a project becomes unavailable
- **THEN** its tile shows an orange dot with "Degraded" and names the affected source

#### Scenario: Project without sources
- **WHEN** a project has no sources
- **THEN** its tile shows a neutral dot with "Not configured" and how to fix it

#### Scenario: Pending events outweigh healthy
- **WHEN** a healthy project has pending warnings only
- **THEN** its tile shows a yellow dot with the pending count

#### Scenario: Pending errors
- **WHEN** a project has pending errors
- **THEN** its tile shows a red dot with the pending count

### Requirement: Tile text is meaningful
The system SHALL NOT show filler text on tiles; every line on a tile SHALL state something the user can act on or that distinguishes the project (for example an unavailable source), and the number of sources SHALL NOT be shown on tiles.

#### Scenario: Healthy tile
- **WHEN** a project is healthy with nothing pending
- **THEN** the tile does not show "Nothing pending" nor a source count

## ADDED Requirements

### Requirement: Manual tile order
The system SHALL let the user reorder project tiles by dragging them, SHALL persist the order across restarts, and SHALL still open a project when a tile is clicked without dragging.

#### Scenario: Drag a tile
- **WHEN** the user drags a tile onto another position and drops it
- **THEN** the tiles are reordered and the order persists after restarting the application

#### Scenario: Plain click
- **WHEN** the user clicks a tile without moving the pointer beyond the drag threshold
- **THEN** the project's screen opens and the order is unchanged

### Requirement: Tile accent
The system SHALL use the project's accent color on its tile (frame and name) and the default accent when none is set; the accent SHALL NOT change the color of the status dot.

#### Scenario: Project with accent
- **WHEN** a project has the accent color `#3FA7D6`
- **THEN** its tile is accented with that color and its status dot keeps the traffic-light color
