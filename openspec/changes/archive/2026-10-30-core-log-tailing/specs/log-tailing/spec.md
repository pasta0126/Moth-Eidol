## Purpose

Defines reliable live incremental reading of log files, including rotation, truncation and unavailability, independent from the UI.

## ADDED Requirements

### Requirement: Incremental reading of new lines
The system SHALL emit as an event each new line appended to a monitored file, in the order it was written, without loading the whole file into memory.

#### Scenario: Line appended
- **WHEN** a line is appended to a monitored file
- **THEN** an event with its text and source is emitted within a delay bounded by the polling interval

#### Scenario: Very large file
- **WHEN** a source whose file is several gigabytes is enabled
- **THEN** monitoring starts without reading the existing content

### Requirement: Initial position
The system SHALL start a new source from the end of the file, so only lines written after it was added are processed.

#### Scenario: New source with prior content
- **WHEN** a source whose file already contains lines is added
- **THEN** those lines are not emitted as events

### Requirement: Resume after restart
The system SHALL persist each source's read position and resume from it at startup, without repeating lines already emitted.

#### Scenario: Lines written while stopped
- **WHEN** the application restarts and the file has grown while it was stopped
- **THEN** only lines after the last recorded position are emitted

#### Scenario: File replaced while stopped
- **WHEN** at startup the file is a different one than recorded or smaller than the saved position
- **THEN** it is treated as a rotation and read from its beginning

### Requirement: Rotation and truncation
The system SHALL detect rotation and truncation of a file and continue monitoring without losing lines from the new file.

#### Scenario: Truncation
- **WHEN** the file size becomes smaller than the read position
- **THEN** reading continues from the beginning of the file

#### Scenario: Rotation by rename
- **WHEN** the file is renamed and another is created at the same path
- **THEN** the previous file's reading is completed if accessible and the new one is read from its beginning

### Requirement: Unavailability and recovery
The system SHALL tolerate a file or network folder disappearing and reappearing, retrying without user intervention and without blocking other sources.

#### Scenario: File disappears
- **WHEN** the file ceases to exist or be accessible
- **THEN** the source becomes "unavailable", is retried at an interval and other sources keep working

#### Scenario: File reappears
- **WHEN** the file becomes accessible again
- **THEN** reading resumes without user intervention

### Requirement: Incomplete lines and encoding
The system SHALL emit a line only when it is complete (terminated by a line break) and SHALL detect UTF-8 and UTF-16 with BOM, accepting CRLF and LF line endings.

#### Scenario: Partially written line
- **WHEN** the file ends in a line without a final line break
- **THEN** that line is not emitted until it is completed

### Requirement: Bounded load
The system SHALL limit the volume read per polling cycle so the system is not degraded by write bursts.

#### Scenario: Write burst
- **WHEN** a file receives a large volume of lines at once
- **THEN** they are processed in batches over successive cycles without blocking the interface
