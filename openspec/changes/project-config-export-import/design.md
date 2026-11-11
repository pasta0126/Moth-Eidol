## Context

Projects are created atomically by the setup wizard. Configuration lives in SQLite; there is no file-based representation. Keywords (`keyword-alerts`) and rules (`regex-alert-rules`) may or may not exist when this is implemented.

## Decisions

- A versioned DTO (`formatVersion: 1`, `kind: "moth-eidol-project"`) decoupled from persistence entities, so the file format survives schema migrations.
- System.Text.Json, indented, stable property order, camelCase.
- Import reuses the wizard's validation and atomic creation path; no new write path into persistence.
- Always import as a new project; no merge/overwrite in this change.
- Sources keep their paths verbatim (they are user/OS specific); the preview checks existence and permissions with the current OS user.
- Keywords and rules are included only if their capabilities are implemented; the DTO leaves optional sections that are ignored when absent (forward compatible).
- Never export secrets; there are none today, and future channel credentials must stay out of this file.

## Risks

- Paths differ between macOS and Windows; mitigated by the preview warning and editing.
- A shared file can carry unexpected values; mitigated by applying the normal validation limits.
