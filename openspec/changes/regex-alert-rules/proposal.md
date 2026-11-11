## Why

Level-based alerts and plain-text keywords (`keyword-alerts`) cannot express rules such as "error code ORA-0\d+", "order id 4\d{6}" or "HTTP 5xx on /api/". Power users need pattern rules that decide whether an entry raises an event, and can also silence known noise.

## What Changes

- Each project can define regular-expression rules.
- A rule is either an **alert** rule (a match raises an event whatever the level) or an **ignore** rule (a match never raises an event, even at error level).
- Rules are validated on save (syntax, length) and matched safely (timeout) so a bad pattern cannot freeze monitoring.
- Rules are managed in project settings with a live "test against sample text / recent entries" helper.
- Alert rules feed the same event grouping, notification channels and frequency policy as keywords.
- Depends on `keyword-alerts` (shares the origin/matched-rule model); implement after it.

## Capabilities

### New Capabilities
- `alert-rules`: per-project regex alert and ignore rules.

### Modified Capabilities
- `project-screen`: settings include a rules editor with a tester.

## Impact

- Core: rule model and validation, matcher with timeout, alert pipeline precedence, persistence migration.
- App: rules editor in project settings.
- No new dependencies (.NET `Regex` with match timeout).
