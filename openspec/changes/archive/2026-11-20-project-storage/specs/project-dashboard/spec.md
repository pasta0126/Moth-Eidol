## ADDED Requirements

### Requirement: Tile storage warning
The system SHALL show on a project's tile a line stating that its storage is over its threshold, with the threshold in MB, while the project's approximate size is over it. The line SHALL NOT change the tile's status dot or status text.

#### Scenario: Healthy project over its threshold
- **WHEN** a healthy project with nothing pending is over its storage threshold of 100 MB
- **THEN** its tile shows a green dot with "Healthy" and a line "Storage over 100 MB"

#### Scenario: Under its threshold
- **WHEN** a project is under its storage threshold
- **THEN** its tile shows no storage line
