## Why

Today only warning and error levels can raise events. Teams often need to be told about specific things that appear in logs at any level, such as "payment declined", "timeout" or a customer id, even when logged as info. Users need project-level keywords that always raise a reviewable event and a notification.

## What Changes

- Each project can define a list of keywords (words or phrases).
- Any log entry of an enabled project whose text contains a keyword raises an event, whatever its level (info included).
- Keyword events appear in the Events section as groups like level-based ones, marked with the matched keyword, with pending/seen state, and count on the tile.
- Keyword events are delivered through the existing notification channels and the project's frequency policy (cooldown or summary).
- Keywords are managed in project settings and offered in the setup wizard alerts step.
- Matching is case-insensitive plain text by default, with an optional per-keyword "match case" flag. Regular expressions stay in the backlog.

## Capabilities

### New Capabilities
- `keyword-alerts`: per-project keywords that raise events from any level.

### Modified Capabilities
- `project-alerts`: keyword matches are also alert-producing, shown and counted.
- `projects`: project configuration includes keywords.
- `project-screen`: settings include a keyword editor.

## Impact

- Core: project model, alert pipeline, event grouping, persistence migration (keywords table, event origin/keyword column).
- App: settings view, wizard alerts step, event rows (keyword badge), tile status.
- No new dependencies.
