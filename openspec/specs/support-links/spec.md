# support-links Specification

## Purpose
Defines the discreet About block in the application settings, the support and feedback links it offers and the public issue templates that receive suggestions and bug reports, without the application sending any data by itself.

## Requirements

### Requirement: About block in the application settings
The system SHALL show an About block at the end of the application settings screen with a short first-person text from the author, signed with the author's handle, and three links: Buy me a coffee, Suggestions & bugs and GitHub. The block SHALL be visually discreet, using small secondary text and plain text links.

#### Scenario: Open the application settings
- **WHEN** the user opens the application settings and scrolls to the end
- **THEN** the About block is shown with the author's text and the three links

#### Scenario: No other placement
- **WHEN** the user uses the dashboard, a project screen or the setup wizard
- **THEN** no support or donation text is shown there

### Requirement: Links open in the browser
The system SHALL open each link in the user's default web browser and SHALL NOT send any application, project or log data with it. Only fixed `https` addresses defined by the application SHALL be opened.

#### Scenario: Click a link
- **WHEN** the user clicks "Buy me a coffee"
- **THEN** the default browser opens the author's Buy Me a Coffee page and the application sends nothing else

#### Scenario: Browser cannot be opened
- **WHEN** the system cannot open the default browser
- **THEN** the application does not fail and tells the user, offering the address to copy

### Requirement: Public feedback through issue templates
The project repository SHALL provide GitHub issue templates for a suggestion and for a bug report, and a template chooser that disables blank issues and links to Buy me a coffee. The bug report template SHALL ask for the application version and operating system and SHALL warn not to paste logs containing sensitive data.

#### Scenario: Suggestions & bugs link
- **WHEN** the user clicks "Suggestions & bugs"
- **THEN** the browser opens the repository's new-issue page showing the suggestion and bug report templates

### Requirement: No data collection
The system SHALL NOT collect or transmit telemetry, usage data or feedback by itself, and the privacy policy SHALL state that the links only open the browser.

#### Scenario: Privacy policy
- **WHEN** a user reads the privacy policy
- **THEN** it states that the application only opens links in the browser and sends no data on its own
