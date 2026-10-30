## Context

`DashboardView` currently shows an inline name box; `ProjectSettingsView` already implements source entry with browse pickers and alert controls. `ProjectService.Create/AddSource/UpdateAlerts` are separate calls, each persisting immediately. `SerilogTextParser` and `LineReader` exist in Core.

## Goals / Non-Goals

**Goals:** guided creation with live validation; atomic result; check against real files; reuse Core, keep logic testable without UI.

**Non-Goals:** editing existing projects through the wizard (settings stays), auto-discovery of log folders, supporting non-Serilog formats, import/export.

## Decisions

- **Draft in Core.** `ProjectDraft` (name, sources, `AlertSettings`) with per-step validation (`ValidateName`, `ValidateSources`, `ValidateAlerts`) returning messages; the view only binds. Alternative (validate in code-behind) rejected: untestable.
- **`LocationInspector` in Core.** Given kind/path/pattern returns an `InspectionResult`: status (ok / missing / unreadable), matching files (name, size, modified), newest file, and up to N latest parsed entries (level, time, first line) using the existing `LineReader`/`EntryAssembler`/parser, reading only the tail (last ~64 KB) so network shares stay cheap. Runs off the UI thread; stale results from a previous path are discarded.
- **Warnings vs errors.** Missing or unreadable path is an error (cannot add); no matches or unrecognized format are warnings (can add), because logs may appear later or the file may be empty.
- **Atomic creation.** `ProjectService.CreateWithSources(draft)` validates everything first, then saves the project, its alert settings and its sources, and if any step fails removes what was already saved (compensating rollback). The project and source repositories are separate stores, so a single SQLite transaction across them is not available without merging them; validating first makes failures rare and the rollback covers the rest.
- **Wizard as a screen of the main window, not a dialog.** Consistent with dashboard/project screens, keeps the footer with the version visible; steps are panels in one `ProjectWizardView` with a step indicator and Back/Next/Cancel.
- **Entry point.** The "New project" tile calls `openWizard`; the inline name panel is removed. After creation the project opens on events (`openProject(project, settings: false)`).
- **Defaults** come from `AlertSettings.Default`; explanation texts are short static strings in the view.

## Risks / Trade-offs

- Slow or unreachable network shares could stall the check → inspection is async with a timeout and a "checking…" state; the user can still continue typing.
- Reading a large file tail for the sample → bounded read from the end.
- Visual verification depends on the screen being available; covered by unit tests of the draft, inspector and atomic creation, plus a manual check by the maintainer.
