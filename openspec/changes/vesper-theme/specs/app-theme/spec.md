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
