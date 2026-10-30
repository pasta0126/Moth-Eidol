## Context

The app runs on each team member's PC and each person configures their own projects. The log folders may be local or on network shares readable with the OS user. See `proposal.md`. Depends on `core-log-tailing` and `serilog-parsing`.

## Goals / Non-Goals

**Goals:**
- A simple, explicit configuration model: project → sources + alert settings.
- Configuration that is persisted and applied without restarting the application.

**Non-Goals:**
- Sharing configuration between users (export/import is backlog).
- Regex rules, per-level routing beyond the warning threshold, or per-source alert settings.
- Evaluating alerts (handled in `project-alerts`).

## Decisions

- **Model**: `Project { Id, Name, Enabled, Sources[], Alerts }` where `Alerts { WarningThreshold: Off | On, Mode: Immediate | Summary, CooldownOrInterval }`. Error and above are always alerting and not configurable. Information never alerts.
- **Defaults**: warnings off, immediate mode with a conservative cooldown; defaults live in settings and can be overridden per project.
- **Sources belong to one project**: the source model from `core-log-tailing` gains a project reference; a source cannot be shared between projects.
- **Hot reload**: saving a project updates the running sources and alert settings without restarting the app.
- **Validation**: unique project name, at least one source, readable path check is advisory (an unreachable path is saved as "unavailable", as in `log-sources`).

## Risks / Trade-offs

- [Per-user configuration can drift across the team] → export/import in backlog.
- [Changing a source's project mid-run could mix history] → history is keyed by project id at the time of the alert.
