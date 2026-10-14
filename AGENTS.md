# AGENTS.md

Guidance for any AI agent (Claude Code, Codex, Cursor, Copilot, Gemini, etc.) working in this repository.

## Project

**MOTH-EIDOL — System Observability Compound**
*For the treatment of undiagnosed system anomalies. Continuous monitoring · Trace analysis · Event detection.*

Cross-platform .NET desktop application to monitor log files.

Goals:

- Monitor log files in real time (active follow, `tail -f` style), local or on network shares reachable with the OS user.
- Raise alerts when user-defined regex rules match.
- Run on Windows (priority), Linux (if easy) and macOS (latest; this is the development machine).

## Stack

- .NET (current LTS) and C#.
- UI: Avalonia (tray icon built in).
- Local storage: SQLite, nothing distributed.
- Tests: xUnit unless a spec says otherwise.

## Language

- **English** is the primary language for everything in the repo: specs, docs, code, identifiers, comments and commit messages.
- The maintainer talks to agents in Spanish; replies to the maintainer are in Spanish, but repo content stays in English.

## Methodology: Spec-Driven Development with OpenSpec

Project definitions live in `openspec/` and **the spec comes before the code**.

1. For any feature or behavior change, first create or update a change proposal in `openspec/changes/<change-name>/`.
2. Implement only what the change's specs and tasks describe.
3. When done, archive the change so the current specs in `openspec/specs/` reflect reality.
4. If code and spec diverge, the spec is the source of truth: fix one of them, never leave them inconsistent.

Do not invent requirements that are not in a spec; if something is missing, propose it as a change.

### Planned changes (MVP order)

| Order | Change | Content |
|---|---|---|
| 1 | `app-shell` | Solution, Avalonia window, tray, background, single instance |
| 2 | `local-persistence` | SQLite, migrations, settings |
| 3 | `core-log-tailing` | Sources, polling tail, rotation, offsets, live viewer |
| 4 | `alert-rules` | Regex rules, throttling, history |
| 5 | `notification-channels` | In-app toast, tray counter, sound, channel chain |

Native OS notifications are out of the MVP; the notifier chain must stay pluggable.

Backlog (not scheduled): `tray-icon-theming` (black/white tray icon per OS theme; macOS template image).

## Versioning and release flow

- Semantic versioning `x.y.z`, starting at `0.1.0`. Version numbers are not pre-assigned to planned changes; the number is decided at release time according to what was actually delivered.
- The version lives in a single place (`Directory.Build.props`); the app always displays it (window title, persistent footer, tray tooltip). Any new window or screen must keep it visible or not hide the footer.
- Each time a change (spec) is finished:
  1. Archive the OpenSpec change.
  2. Bump the version according to semver and what was delivered.
  3. Commit, push, and push the git tag `vX.Y.Z`.
  4. **Tell the maintainer** so they can run the application manually and verify it.
- Do not start the next change before the maintainer has had the chance to verify.

## Conventions

- Keep monitoring and alert logic independent from the UI and the platform (core testable without a UI).
- Isolate OS-specific code (tray, autostart, paths, permissions) behind abstractions.
- Small, focused changes; do not mix refactors with features.
- Commit messages in the imperative mood; no unrelated files in a commit.

## UI theme

- Single dark theme defined in `src/MothEidol.App/Themes/MothTheme.axaml` (palette taken from the moth artwork: obsidian, amber, copper, cream, one turquoise accent).
- Views must use the `Moth*Brush` resources (`DynamicResource`); never hard-code colors.
- Main combination: obsidian background + intense amber primary + cream text; turquoise only for secondary/info details.
- Semantic levels: info `MothInfoBrush`, warning `MothWarningBrush`, error `MothErrorBrush`.

## Commands

```sh
dotnet build
dotnet test
dotnet run --project src/MothEidol.App
```

In VS Code: `F5` ("Moth-Eidol (debug)") builds and debugs the app; the task "run (script)" launches `scripts/run.sh`.

Shortcut to build and launch the app: `scripts/run.sh` (macOS/Linux) or `scripts\run.cmd` (Windows).

## Notes for agents

- Read this file and the relevant specs in `openspec/` before changing code.
- Do not add dependencies without justifying them in the spec or change description.
- Do not touch `LICENSE` or other tools' configuration files unless asked.
