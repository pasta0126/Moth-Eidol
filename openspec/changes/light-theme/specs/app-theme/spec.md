## Purpose
Defines the visual themes the application offers, how the user chooses one and how the choice is applied and remembered.

## ADDED Requirements

### Requirement: Available themes
The system SHALL provide a dark theme and a light theme, and every screen, window and toast SHALL be readable in both.

#### Scenario: Light theme active
- **WHEN** the light theme is active
- **THEN** all screens, windows and toasts use the light palette and no text or control becomes unreadable

### Requirement: Theme selection
The system SHALL let the user choose the theme in the application settings and SHALL apply the choice immediately, without restarting.

#### Scenario: Change theme
- **WHEN** the user selects a different theme in the application settings
- **THEN** the whole application changes to that theme right away

### Requirement: Theme persistence
The system SHALL remember the chosen theme between runs.

#### Scenario: Restart with a chosen theme
- **WHEN** the user restarts the application after choosing the light theme
- **THEN** the application starts in the light theme

### Requirement: Semantic level colors
The system SHALL keep info, warning and error recognizable and distinguishable from each other in both themes.

#### Scenario: Levels in the light theme
- **WHEN** events of different levels are shown in the light theme
- **THEN** each level keeps its distinct color with enough contrast against the background
