## Why

The real Serilog logs of the monitored applications are the best test material, but they contain personal data (employee emails, tax ids), infrastructure details (hosts, IPs, developer paths) and company names, so they cannot be committed. We also need a way to exercise live tailing (append, daily rollover, truncation, partial lines) without waiting for real applications to write.

## What Changes

- An anonymizer that turns real logs into small, committable fixtures: deterministic pseudonyms for sensitive data, fictional project and company names, shifted dates, structure and levels preserved.
- A simulator that writes log files like a Serilog application would: replaying fixtures, generating new plausible entries from message templates learned from the fixtures, and reproducing rotation scenarios.
- Anonymized fixtures committed under `tests/fixtures/logs/`.
- Specific sensitive names (real company, project names, user names) live in a git-ignored configuration file, never in the repo.

## Capabilities

### New Capabilities
- `log-sample-tooling`: anonymization of real logs into fixtures and simulation of live log writing.

### Modified Capabilities

## Impact

- New console project `tools/MothEidol.LogSim` and its test project, added to the solution. It uses only the .NET base library, no new packages.
- `.gitignore` gains an exception so fixture `.log` files are versioned.
- Feeds the tests and manual verification of `core-log-tailing`, `serilog-parsing` and `project-alerts`.
- No change to the application itself.
