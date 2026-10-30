## Purpose

Defines the development tooling that turns real logs into publishable fixtures and simulates live log writing for testing the monitoring features.

## ADDED Requirements

### Requirement: Anonymization of sensitive data
The tool SHALL replace email addresses, Spanish tax identifiers, IPv4 addresses, Windows user names in paths and URL hosts with consistent fictional values, and SHALL replace the terms listed in a private configuration (company, project and user names) with fictional ones.

#### Scenario: Consistent pseudonyms
- **WHEN** the same email appears several times in the input
- **THEN** it is replaced by the same fictional email everywhere in the output

#### Scenario: Private terms
- **WHEN** the private configuration lists a company name and a project name
- **THEN** every occurrence in messages, stack traces and file names is replaced, including the output file names

#### Scenario: Repository stays clean
- **WHEN** the tool is built and committed
- **THEN** neither its source nor its fixtures contain the real terms of the private configuration

### Requirement: Preserved structure
The tool SHALL preserve entry format, levels, multi-line entries, line endings, encoding and relative time between entries.

#### Scenario: Stack trace
- **WHEN** an error entry with a stack trace is anonymized
- **THEN** the output keeps the header and the continuation lines in the same order

### Requirement: Date shifting
The tool SHALL shift every date in the content and in file names by one constant offset.

#### Scenario: Daily file names
- **WHEN** a file named `<Project>20261028.log` is anonymized with an offset
- **THEN** the output file name carries the shifted date and the entries inside carry consistently shifted timestamps

### Requirement: Size reduction
The tool SHALL keep all entries of warning level and above, up to a configurable cap, and a bounded sample of lower levels.

#### Scenario: Large input
- **WHEN** a log with thousands of information entries is anonymized
- **THEN** the output keeps its warnings and errors and a bounded sample of information entries

### Requirement: Leak check
The tool SHALL verify its output against the private terms and the original dates and SHALL fail if any appears.

#### Scenario: Residual term
- **WHEN** the output still contains a term of the private configuration
- **THEN** the run reports the file and the term and exits with an error

### Requirement: Event generation
The simulator SHALL generate new entries in the same format from message templates learned from sample logs, with a configurable rate and a seed for reproducible output.

#### Scenario: Reproducible output
- **WHEN** the generator runs twice with the same seed and samples
- **THEN** the produced messages and levels are identical

### Requirement: Live writing scenarios
The simulator SHALL write to a target log file as a running application would, and SHALL reproduce append, partial line, daily rollover to a new file name, truncation, rename rotation and disappearance with reappearance, without locking the file against readers.

#### Scenario: Partial line
- **WHEN** the partial scenario runs
- **THEN** a line is written without its line break and completed after a delay

#### Scenario: Daily rollover
- **WHEN** the rollover scenario runs
- **THEN** writing continues in a new file named with the next date while the previous file stays untouched
