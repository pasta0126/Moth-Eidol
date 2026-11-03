## 1. Core and platform

- [x] 1.1 Resolve the target (file, newest followed file or folder) for a source, with an existence check off the UI thread and a timeout
- [x] 1.2 Add the file-manager abstraction with macOS, Windows and Linux implementations (no shell, path as argument)
- [x] 1.3 Report unavailable location and missing file manager as user-facing messages

## 2. UI

- [x] 2.1 "Open folder" button in the Tail toolbar, with a folder chooser when sources span several folders
- [x] 2.2 Open action per source in project settings

## 3. Validation

- [x] 3.1 Unit-test target resolution and error cases with a fake file manager
- [x] 3.2 Build and run the tests
- [ ] 3.3 Check manually on macOS and Windows, including a disconnected share (deferred to the maintainer)
