# Changelog

All notable changes to Moth-Eidol, newest first. Versions follow [semantic versioning](https://semver.org/).
Only versions published to the Microsoft Store get an entry; versions in between are folded into the next published one.
Each entry is written for the people who use the app; the technical detail lives in `openspec/`.

## [1.8.0] - 2026-11-10

### Added
- Each project can have its own accent color, chosen in its settings from 16 colors or typed as a hex value (for example `#3FA7D6`), with a button to return to the default. The color appears on the project's tile.
- Project tiles can be reordered by dragging them; the order is remembered between runs and new projects go last.

### Changed
- The status on each tile is now a colored dot with its text: green when healthy, yellow with pending warnings, orange when a source is unavailable, red with pending errors, and grey when not configured or disabled. The most severe one is shown.
- Healthy tiles no longer show "Nothing pending" or the number of sources; any other line on a tile now says something you can act on.
- Tiles are no longer sorted by name automatically. Your existing projects keep their current (alphabetical) order until you move one.

## [1.7.0] - 2026-11-05

### Added
- On Windows, alerts arrive as native Windows notifications, with their history in the Action Center; clicking one opens the app.
- An "Alert sound" setting to choose between sound and silent.
- A Raw view in the tail and in the event detail that shows the plain text, so you can select it exactly as you want (drag, double or triple click, Shift+click, select all) and copy it.
- A Copy button on every occurrence of an event that copies its whole detail: the lines before the error, the message and the stack trace.
- An About block at the end of Settings with a few words from the author and links to support the project, send suggestions or bug reports, and visit the author's GitHub. The links only open your browser; the app sends nothing by itself.
- Suggestion and bug report forms on the project's GitHub page.

### Changed
- The actions that apply to the text you are reading (Word wrap, Open folder) now live in a small bar at the top right of the tail and of the event detail.
- On Windows the native notification replaces the in-app toast by default; the toast stays available and is used if the native one cannot be shown.
- A little space above the footer so it no longer touches the content.
- The privacy policy now explains that the About links only open the browser.

## [1.3.1] - 2026-11-05

### Added
- Text is selectable and copyable everywhere: tail lines (across several lines), event details, stack traces, tiles and settings. Tiles and event rows still open when you click them; selection starts by dragging.
- Word wrap in the tail and in the event detail, on by default and remembered between runs. Turn it off to go back to horizontal scrolling.
- An Open folder button in the tail and in each log source of the project settings. It shows the log file in your file manager, and tells you clearly when the folder is missing or unreachable (for example, a disconnected network share).

## [1.0.0] - 2026-11-02

First published version, also submitted to the Microsoft Store.

### Added
- A dashboard with one status tile per project, showing whether anything is pending, the events of the last 24 hours and their trend.
- A guided wizard to create a project: name, log location (checked live, with a preview of the entries it understands), alerts and review.
- Real-time follow of log files and folders (a file, or a folder plus a pattern that follows the newest file), local or on network shares, surviving rotation and restarts.
- Alerts by log level: info never alerts, warnings alert depending on the project, errors always alert. Alert frequency is set per project: immediate with a cooldown, or a periodic summary.
- Events grouped by cause with readable titles, relative times, sparklines, and New and Regressed badges, with a pending and seen state.
- An event detail with an ordered stack trace, the log lines that came right before the error, an occurrence timeline and a button to copy a summary.
- A live tail colored by level, with level filter, pause and clear.
- Notifications: in-app toast, tray icon with an unread counter, and sound, each switchable on its own.
- Runs from the system tray, keeps monitoring when the window is closed, allows a single instance per user and can start with your session.
- Self-contained downloads for Windows, macOS and Linux that need no installer, and a Microsoft Store package.
- Everything stays on your computer: no accounts, no telemetry.
