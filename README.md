# MOTH-EIDOL

**SYSTEM OBSERVABILITY COMPOUND**

*For the treatment of undiagnosed system anomalies.*
Continuous monitoring · Trace analysis · Event detection

Cross-platform .NET desktop application that tails log files, matches them
per project and raises alerts by log level. Project definitions live
in `openspec/` (spec-driven development); contributor and agent guidance is in
`AGENTS.md`.

## Test logs and simulator

`tools/MothEidol.LogSim` anonymizes real logs into committable fixtures and
simulates a running application writing logs (no extra packages needed).

```sh
# Anonymize logs/<project>/*.log into tests/fixtures/logs (needs the private logs/anonymize.json,
# see tools/MothEidol.LogSim/anonymize.sample.json; the run fails if anything sensitive survives)
dotnet run --project tools/MothEidol.LogSim -- anonymize --input logs --output tests/fixtures/logs --config logs/anonymize.json

# Write new plausible entries, a daily rollover, a partial line, truncation, rotation or a disappearing file
dotnet run --project tools/MothEidol.LogSim -- generate --target /tmp/live.log --samples tests/fixtures/logs --count 50 --mix INF=70,WRN=20,ERR=10
dotnet run --project tools/MothEidol.LogSim -- rollover --target /tmp/logs --name Demo --samples tests/fixtures/logs
```

Run without arguments to list every command. `logs/` is git-ignored: real logs and the private
term list never leave the machine.
