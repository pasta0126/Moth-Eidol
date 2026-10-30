## Why

Creating a project today asks only for a name and then drops the user on a settings screen where sources and alert options must be discovered and filled in by hand. New users do not know that a folder needs a pattern, whether the app can read the location, or what the alert options mean. A guided wizard collects every value the app needs, checks it against the real files, and leaves a working project at the end.

## What Changes

- The "New project" tile starts a step-by-step wizard instead of the name-only form.
- Steps: name, log location (file or folder + pattern, one or more sources), alerts, review. Each step validates before advancing; back and cancel are always available.
- The location step checks the path live: exists, readable, which files match the pattern, which one is the newest, and a sample of the entries parsed from it with the detected levels, so the user sees that the logs are understood before saving.
- Nothing is saved until the final step; cancelling leaves no trace.
- On finish the project is created with its sources and alert settings in one go and its screen opens on the events section.
- Project settings remain the place to edit everything afterwards.

## Capabilities

### New Capabilities
- `project-wizard`: guided creation of a project (steps, validation, log location preview, atomic creation).

### Modified Capabilities
- `project-dashboard`: "Create from the dashboard" now starts the wizard and opens the project on its events section.

## Impact

- `MothEidol.Core`: a location inspector (resolves the matching files, newest file, parsed sample) and a project draft with step validation; `ProjectService` gets an atomic create-with-sources operation.
- `MothEidol.App`: wizard view; dashboard "New project" tile launches it; no new dependencies.
- Tests for the inspector, draft validation and atomic creation.
