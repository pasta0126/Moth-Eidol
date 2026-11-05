# Store listing — English (United States)

Copy each block into the matching Partner Center field.

## Description
```
Moth-Eidol is a log tracking & monitoring app for people who need to know when something goes wrong, without reading raw log files all day.

Organize monitoring by project: give each one a name and the folders and file patterns to watch. Moth-Eidol follows the newest matching file in real time, on local folders or network shares reachable with your Windows user.

Alerts follow the log level: info is shown but never alerts, warnings alert depending on the project settings, and errors always alert. Events are grouped and readable, with clear titles instead of raw lines, relative times, trends and "New" and "Regressed" badges. Open any event to see its ordered stack trace, the log lines that came right before the error and an occurrence timeline.

Moth-Eidol keeps working from the system tray, can start with your session, and notifies you in-app, with a tray counter and sound.

PRIVATE BY DESIGN
Everything stays on your computer. Moth-Eidol has no accounts, no telemetry and never sends your logs anywhere. It reads files with your own Windows permissions and never modifies them.

Built for Serilog-style text logs. Open source.
```

## What's new in this version
Do not write it by hand: take it from `CHANGELOG.md`. For the version being submitted run
`powershell -File scripts\changelog-section.ps1 -Version X.Y.Z` and paste the output (it fails when the entry is missing or too long).
For the first submission (1.0.0) it can be left blank.

## Product features (one per line)
- Dashboard with one status tile per project
- Real-time follow of log files and folders, local or on network shares
- Alerts by log level, configurable per project
- Events grouped by cause with readable titles and relative times
- Trends, sparklines and New / Regressed badges
- Ordered stack traces, context before the error and occurrence timeline
- Copy an event summary with one click
- Live tail with colors per level and level filter
- Immediate alerts with cooldown or periodic summaries
- In-app notifications, tray counter and sound
- Runs from the system tray and can start with your session
- 100% local: no accounts, no telemetry

## Screenshots (Desktop), in this order
1. `screenshots/01-dashboard.png` — Status at a glance: one tile per project.
2. `screenshots/02-events.png` — Events grouped by cause, with trends and badges.
3. `screenshots/03-event-detail.png` — Stack trace, context before the error and timeline.
4. `screenshots/04-live-tail.png` — Live tail with colors and filters.

## Store display images
- 1:1 App tile icon (300x300): `app-tile-icon-300x300.png`
- Leave everything else empty (poster art, box art, 150x150, 71x71, super hero, Xbox images, trailers).

## Short description
```
Log tracking & monitoring. Follow your log files in real time and get alerted per project, with readable events instead of raw lines.
```

## Keywords
`log monitoring`, `log viewer`, `tail`, `serilog`, `observability`, `alerts`, `developer tools`

## Copyright and trademark info
`© 2026 Pasta0126`

## Developed by
`Pasta0126`
