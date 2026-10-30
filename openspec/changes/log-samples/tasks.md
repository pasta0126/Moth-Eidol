## 1. Anonymizer

- [x] 1.1 Console project `tools/MothEidol.LogSim` with `anonymize` command and argument parsing
- [x] 1.2 Generic rules: emails, NIF/NIE, IPv4, Windows user paths, URL hosts
- [x] 1.3 Private configuration (`logs/anonymize.json`): terms, project renames, date offset; sample file documented
- [x] 1.4 Date shifting across headers, ISO timestamps, file names and embedded file names
- [x] 1.5 Trimming and leak check

## 2. Simulator

- [x] 2.1 Template learning and `generate` with seed
- [x] 2.2 `replay` with rate and Serilog-style file sharing
- [x] 2.3 Scenarios: `rollover`, `truncate`, `rotate`, `partial`, `disappear`

## 3. Fixtures and verification

- [x] 3.1 Add test project; unit tests for each rule, leak check, determinism and scenarios
- [x] 3.2 Generate fixtures into `tests/fixtures/logs/` and review them manually
- [x] 3.3 Allow fixture `.log` files in `.gitignore`; document usage in the README
- [ ] 3.4 Close the change: archive, bump the version (semver) if warranted, commit, push and tag it
