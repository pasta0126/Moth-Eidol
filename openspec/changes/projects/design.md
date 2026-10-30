## Context

The app runs on each team member's PC and each person configures their own projects. The log folders may be local or on network shares readable with the OS user. See `proposal.md`. Depends on `core-log-tailing` and `serilog-parsing`.

## Goals / Non-Goals

**Goals:**
- A home screen answering "what needs attention" at a glance, built so `project-alerts` can add the pending state without redesign.
- A simple, explicit configuration model: project → sources + alert settings, edited in one place.
- Configuration persisted and applied without restarting the application.

**Non-Goals:**
- The events list, "seen" handling and pending state (delivered in `project-alerts`).
- Sharing configuration between users (export/import is backlog).
- Regex rules, or per-source alert settings.
- Evaluating alerts (handled in `project-alerts`).

## Decisions

- **Navigation**: the dashboard is the home; a tile opens the project screen; a back control returns to the dashboard; application settings are reachable from the dashboard. The footer with the version stays visible on every screen.
- **Model**: `Project { Id, Name, Enabled, Sources[], Alerts }` where `Alerts { WarningThreshold: Off | On, Mode: Immediate | Summary, CooldownOrInterval }`. Error and above always alert and are not configurable; information never alerts. Warnings default to off and the mode to immediate with a conservative cooldown; defaults live in settings.
- **Creation flow**: a project can exist with only a name ("Not configured"); its parameters are set afterwards on its own screen. This avoids a long form on the dashboard.
- **Tile status (this change)**: derived from source health only: Healthy, Degraded (a source unavailable or in read error), Not configured (no sources), Disabled. `project-alerts` extends the tile with the pending count and level.
- **Project tail**: the existing viewer scoped to the project's sources, with an "all sources" option and a source filter; lines are collected for every project even when not displayed.
- **Sources belong to one project**: the source model gains a project reference; a source cannot be shared between projects.
- **Migration**: sources created before projects exist are moved into a project named "Unassigned" so no monitoring and no read position is lost.
- **Hot reload**: saving a project updates the running sources and alert settings without restarting the app.
- **Validation**: unique, non-empty project name; unreachable paths are saved as "unavailable", as in `log-sources`.

## Risks / Trade-offs

- [Per-user configuration can drift across the team] → export/import in backlog.
- [Removing the global Live and Sources tabs changes a recently released screen] → the same viewer and source controls move into the project screen, and existing sources are migrated.
- [Many projects make the dashboard long] → tiles wrap in a grid and are ordered by name.
