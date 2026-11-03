## 1. Core

- [ ] 1.1 Add keyword model and validation (trim, length, duplicates, limit) to the project
- [ ] 1.2 Migration and persistence for keywords and for the event origin/matched keywords
- [ ] 1.3 Match keywords in the alert pipeline on the full entry text; record keyword events with their own group key
- [ ] 1.4 Deliver keyword events through the frequency policy and notification channels, naming the keyword
- [ ] 1.5 Tile status: notice state for pending info-level keyword events

## 2. UI

- [ ] 2.1 Keyword editor in project settings with pending count per keyword
- [ ] 2.2 Optional keywords field in the wizard alerts step (atomic creation)
- [ ] 2.3 Keyword badge in event rows and detail; `MothInfoBrush` for the notice state

## 3. Validation

- [ ] 3.1 Unit tests: validation, case/match-case, continuation lines, multiple keywords, disabled project, grouping separation, cooldown/summary
- [ ] 3.2 Build and run the tests
- [ ] 3.3 Check manually with the log simulator that an info line with a keyword raises an event and a notification (deferred to the maintainer)
