# local-persistence Specification

## Purpose
Defines the application's local storage: where data is kept, how the schema evolves and how it behaves on failure.

## Requirements

### Requirement: Per-user local storage
The system SHALL store all its data in a local database in the user's application data directory, without depending on any network service.

#### Scenario: First startup
- **WHEN** the application starts and no database exists
- **THEN** the database is created in the user's data directory with the current schema

#### Scenario: Restart
- **WHEN** the application restarts
- **THEN** data saved in the previous run is available

### Requirement: Schema migration
The system SHALL version the schema and apply pending migrations at startup without losing existing data.

#### Scenario: Older database version
- **WHEN** the application starts with a database of an older schema version
- **THEN** pending migrations are applied and previous data is preserved

#### Scenario: Newer database version
- **WHEN** the database has a schema version newer than the supported one
- **THEN** the application does not modify it and informs the user of the incompatibility

### Requirement: Application settings
The system SHALL persist user settings as key-value pairs with defined default values.

#### Scenario: Unset setting
- **WHEN** a setting the user has never changed is read
- **THEN** its default value is returned

#### Scenario: Modified setting
- **WHEN** the user changes a setting and restarts the application
- **THEN** the modified value is preserved

### Requirement: Recovery from a corrupt database
The system SHALL keep a copy of a corrupt or unreadable database and create a new one, informing the user, instead of failing at startup.

#### Scenario: Unreadable file
- **WHEN** the database cannot be opened due to corruption
- **THEN** it is renamed to a backup copy, a new database is created and the user is informed
