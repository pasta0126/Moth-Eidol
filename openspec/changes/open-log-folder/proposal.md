## Why

When investigating an event, users often need the raw log file or its folder (to open it in an editor, attach it, or compare with other files). Today they must find the path by hand. A button to open the log folder in the OS file manager saves that step.

## What Changes

- The project Tail section gets an "Open folder" button that opens the folder of the selected source (or the newest followed file's folder when "All sources" is selected, offering the choice when several folders exist).
- The project Settings source list gets the same action per source.
- Where the OS supports it, the followed file is revealed (selected) in the file manager; otherwise its folder is opened.
- Missing or unreachable folders (for example a disconnected network share) show a clear message instead of failing silently.
- The path of the source can also be copied (consistent with `selectable-text`).

## Capabilities

### New Capabilities
- `open-log-folder`: open a source's folder or file in the OS file manager.

### Modified Capabilities
- `project-screen`: tail and settings include the open-folder action.

## Impact

- New platform abstraction for "reveal path in file manager" with macOS, Windows and Linux implementations, isolated from the core per AGENTS.md conventions.
- UI: `TailView`, `ProjectSettingsView`. No storage changes, no new dependencies.
