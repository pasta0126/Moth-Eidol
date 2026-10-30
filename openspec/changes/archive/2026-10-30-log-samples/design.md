## Context

`logs/` holds real logs from six applications (git-ignored, one subfolder per project). Sensitive content found: employee emails, Spanish tax ids (NIF/NIE), internal hosts and IPs, Windows paths with the developer's user name, and company/project names inside messages, stack traces and file names. See `proposal.md`.

## Goals / Non-Goals

**Goals:**
- Fixtures that are safe to publish and still exercise the real structure (levels, stack traces, long lines, emojis, CRLF, daily files).
- A reproducible simulator usable from tests and by hand on macOS and Windows.

**Non-Goals:**
- Anonymizing arbitrary free text with NLP; unknown sensitive names are handled by extending the private configuration.
- A product feature: the tool is development support and is not shipped with the app.

## Decisions

- **Two layers of rules**: generic rules are in code (emails, NIF/NIE, IPv4, `Users\<name>` paths, URL hosts); specific names (company, project names, user names, internal domains) are read from a private JSON file (`logs/anonymize.json`, ignored by git). This keeps real names out of the repository, including out of the tool's source.
- **Deterministic pseudonyms**: each distinct value maps to a stable fake (`user001@example.test`, `10.0.0.x` from a reserved range, `00000001A`-style ids) via a per-run table seeded from the config, so repeated values stay consistent inside a fixture.
- **Date shift**: every timestamp (entry headers, ISO timestamps in messages, `yyyyMMdd` in file names, `ddMMyyyy` in embedded file names) is shifted by one constant offset, preserving relative times and the daily-file naming.
- **Trimming**: fixtures keep all entries of warning level and above, up to a cap, plus a bounded sample of information entries, so they stay small.
- **Leak check**: after writing, the tool scans its output for every private-config term and every original date; any hit fails the run and nothing is considered publishable.
- **Simulator**: scenarios are explicit commands (`replay`, `generate`, `rollover`, `truncate`, `rotate`, `partial`, `disappear`) writing with `FileShare.ReadWrite | Delete` like Serilog does. `generate` learns message templates by replacing numbers, quoted values, GUIDs and timestamps with placeholders, then fills them with random values of the same shape; it takes a seed for deterministic output.
- **Packaging**: console project under `tools/` referenced by the test projects; no third-party packages, minimal hand-written argument parsing.

## Risks / Trade-offs

- [Residual sensitive text the rules do not know] → leak check against the private config plus manual review of fixtures before commit.
- [Templates from a small sample generate repetitive output] → more samples can be added over time; the learning step is data-driven.
- [A tool in the solution slows builds slightly] → it is a small project without dependencies.
