## 1. Core and platform

- [ ] 1.1 Resolve the target (file, newest followed file or folder) for a source, with an existence check off the UI thread and a timeout
- [ ] 1.2 Add the file-manager abstraction with macOS, Windows and Linux implementations (no shell, path as argument)
- [ ] 1.3 Report unavailable location and missing file manager as user-facing messages

## 2. UI

- [ ] 2.1 "Open folder" button in the Tail toolbar, with a folder chooser when sources span several folders
- [ ] 2.2 Open action per source in project settings

## 3. Validation

- [ ] 3.1 Unit-test target resolution and error cases with a fake file manager
- [ ] 3.2 Build and run the tests
- [ ] 3.3 Check manually on macOS and Windows, including a disconnected share (deferred to the maintainer)
