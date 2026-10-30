# log-entries Specification

## Purpose
Defines how raw log lines are turned into structured entries with timestamp, level and message, including multi-line entries such as stack traces.

## Requirements

### Requirement: Entry parsing
The system SHALL parse each entry header of the Serilog text format `yyyy-MM-dd HH:mm:ss.fff +zz:zz [LVL] message` into a timestamp, a level and a message.

#### Scenario: Valid header
- **WHEN** the line `2026-10-28 10:49:09.258 +01:00 [INF] Concurrency limiter initialized` is read
- **THEN** an entry is produced with that timestamp, level Information and the remaining text as message

#### Scenario: Unknown level token
- **WHEN** a header carries a level token the parser does not know
- **THEN** the entry is produced with level Unknown and no text is lost

### Requirement: Multi-line grouping
The system SHALL append lines that are not entry headers to the preceding entry, so a stack trace stays attached to its error.

#### Scenario: Exception with stack trace
- **WHEN** an `[ERR]` header is followed by lines without a timestamp
- **THEN** a single entry is emitted whose message contains the header text and all the following lines in order

#### Scenario: Lines before the first header
- **WHEN** the first lines read do not start with a header
- **THEN** they form an entry with unknown level and timestamp

### Requirement: Timely emission
The system SHALL emit an entry once the next header arrives or a short idle time has elapsed, so the last entry is not held back indefinitely.

#### Scenario: Last entry of a burst
- **WHEN** an entry is complete and no further line arrives
- **THEN** the entry is emitted after the idle timeout

### Requirement: Bounded entry size
The system SHALL limit the stored length of an entry, keeping the header, so pathological traces cannot exhaust memory.

#### Scenario: Oversized entry
- **WHEN** an entry exceeds the maximum length
- **THEN** its text is truncated and marked as truncated, and monitoring continues

### Requirement: Pluggable parser
The system SHALL expose the parser as an abstraction so other formats can be supported without changing consumers.

#### Scenario: Alternate parser registered
- **WHEN** another parser implementation is registered for a source
- **THEN** its entries flow through the same channel as Serilog entries
