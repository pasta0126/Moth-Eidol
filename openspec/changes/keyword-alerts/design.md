## Context

`AlertSettings` decides alert level by entry level; `AlertPipeline` records every alert-level entry into groups and shapes deliveries with the frequency policy.

## Decisions

- Keywords belong to the project (new `Keywords` collection next to `AlertSettings`), stored in their own table with a migration. Rationale: a list with flags does not fit the existing settings record.
- Matching runs in the pipeline on the full entry text (header plus continuation lines), case-insensitive ordinal by default, substring semantics (not whole word). Assumption recorded; whole-word/regex is future work.
- An entry is alert-producing if level-based OR keyword-matched. If both, one occurrence is recorded (never duplicated) and it carries the matched keywords.
- Grouping: keyword events keep the entry level but the group key includes an origin flag so a keyword group never merges with a plain level group of the same message. The group stores the matched keyword(s) for display.
- Event level for tile status: info-level keyword events show a distinct "notice" state using `MothInfoBrush`, below warning in severity.
- Delivery goes through the existing frequency policy and channels unchanged; summaries mention keyword hits count.
- Keyword list limits: up to 50 per project, 1–200 chars, trimmed, duplicates (case-insensitive) rejected.

## Risks

- Very common keywords flood events; the cooldown protects notifications but not the events list, so the UI should show the match count per keyword.
- Matching cost on busy logs: simple ordinal search, bounded by entry length cap.
