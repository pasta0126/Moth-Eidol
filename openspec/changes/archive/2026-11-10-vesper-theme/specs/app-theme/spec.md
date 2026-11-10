## Purpose
Defines the visual themes the application offers (Moth, dark, and Vesper, warm light), how the user chooses one and how the choice is applied and remembered.

## ADDED Requirements

### Requirement: Available themes
The system SHALL provide two themes, Moth (dark) and Vesper (warm ivory, navy and gold), and every screen, window and toast SHALL be readable in both.

#### Scenario: Vesper theme active
- **WHEN** the Vesper theme is active
- **THEN** all screens, windows and toasts use the Vesper palette and no text or control becomes unreadable

### Requirement: Theme selection
The system SHALL let the user choose the theme in the application settings and SHALL apply the choice immediately, without restarting.

#### Scenario: Change theme
- **WHEN** the user selects a different theme in the application settings
- **THEN** the whole application changes to that theme right away

### Requirement: Theme persistence
The system SHALL remember the chosen theme between runs and SHALL use Moth until the user chooses otherwise.

#### Scenario: Restart with a chosen theme
- **WHEN** the user restarts the application after choosing Vesper
- **THEN** the application starts in Vesper

#### Scenario: New installation
- **WHEN** the application runs for the first time
- **THEN** the Moth theme is used

### Requirement: Semantic level colors
The system SHALL keep info, warning and error recognizable, distinguishable from each other and legible against the background in both themes.

#### Scenario: Levels in Vesper
- **WHEN** events of different levels are shown in Vesper
- **THEN** each level keeps its distinct color with enough contrast against the ivory and cream backgrounds

### Requirement: Readable project accent
The system SHALL keep the project name on its tile legible in both themes: the chosen accent color is used unchanged for the tile stripe and frame, and, when used as text, it SHALL be shifted toward the text color only as far as needed to reach a contrast of 4.5:1 against the tile background.

#### Scenario: Pale accent in Vesper
- **WHEN** a project has the accent `#E8E2D4` and the Vesper theme is active
- **THEN** the stripe and frame show `#E8E2D4` and the project name is shown in a darker shade that is clearly readable on the cream tile

#### Scenario: Accent already legible
- **WHEN** the accent already has at least 4.5:1 against the tile background
- **THEN** the project name uses it unchanged

### Requirement: Tray icon independent of the theme
The system SHALL NOT change the tray icon with the application theme.

#### Scenario: Vesper with the tray icon
- **WHEN** the Vesper theme is active
- **THEN** the tray icon and its unread counter look the same as in Moth
