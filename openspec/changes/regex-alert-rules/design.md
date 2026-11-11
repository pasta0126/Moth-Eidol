## Context

`keyword-alerts` introduces an alert origin (level vs keyword) and matched-keyword data on events. Rules are the generalization with patterns and an ignore kind.

## Decisions

- Separate `Rules` collection and table from keywords: keywords stay simple plain text; rules carry kind, pattern and flags.
- .NET `Regex` with `RegexOptions.CultureInvariant` and a match timeout (e.g. 100 ms); compiled rules are cached per project and rebuilt on hot reload.
- Precedence: ignore rules first, then alert rules/keywords/level. Ignored entries never create events but still reach the live tail.
- Event grouping key includes the origin `rule` and rule id, like keywords.
- The tester evaluates against the in-memory recent entries of the project tail; it never records events.
- No named-group extraction or templates in this change (future work).

## Risks

- Catastrophic backtracking: mitigated by timeout and by flagging the rule.
- Ignore rules can hide real errors; the settings show a per-rule "ignored count" so the user can audit them.
- Overlap with keywords may confuse users; the UI should explain "keyword = plain text, rule = pattern".
