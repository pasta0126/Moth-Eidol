## Context

Releases are tagged `vX.Y.Z` (`v0.1.0` to `v0.10.0`, then `v1.1.0` onward, with `1.3.1`, `1.4.0` to `1.7.0` after). Only versions published to the Store get an entry (`1.0.0`, `1.3.1`, and `1.7.0` as the next one). Each finished change is archived under `openspec/changes/archive/` with a proposal that already says what changed for the user. The Store listing lives in `packaging/store-listing/listing-en-US.md` and its "What's new in this version" section is currently "Leave blank (first submission)". That Store text is typed or pasted in Partner Center at submission; it is not part of the MSIX package.

## Goals / Non-Goals

**Goals:**
- One source of truth for what changed in each version.
- Make the Store "What's new" text a by-product of the changelog, not extra writing at submission time.

**Non-Goals:**
- Showing the changelog or a "What's new" screen inside the application.
- Generating the changelog automatically from commit messages.
- Localizing the Store text (English only for now).

## Decisions

- **Format:** `CHANGELOG.md` in the "Keep a Changelog" style, newest version first, with `Added`, `Changed` and `Fixed` sections and the release date. User-facing wording ("Raw text view to copy the exact log text"), not commit or spec names.
- **Scope of the history:** only Store-published versions. Intermediate tagged versions (`1.1.0` to `1.3.0`, `1.4.0` to `1.6.1`) are folded into the next published entry, and the development versions `0.1.0` to `0.10.0` into `1.0.0`. Fixes to bugs introduced and fixed between two published versions (the `1.6.1` startup crash, the `1.3.1` wrap trimming) are not listed because no published version had them. If the version to submit changes before submission, the entry heading is renamed.
- **Source of the history:** the git tags for the versions and dates, and the proposals of the archived changes for the wording.
- **Process:** the entry is written when a change is finished, before bumping the version, so the tag always matches a documented version. `AGENTS.md` records the new order.
- **Store text:** a helper script in `scripts/` prints the changelog section of a given version in plain text suitable for Partner Center and fails if it exceeds the Store limit (about 1,500 characters). The text is pasted at submission. The "Build MSIX" workflow may attach it as an artifact.
- **Optional:** the same helper can provide the body of a GitHub release.

## Open questions

- **MSIX integration limit:** the Store field is not part of the package, so it cannot be "deployed inside the MSIX". If it turns out that no automated hand-over to Partner Center is possible (submission is manual), the fallback is just `CHANGELOG.md` plus the extraction helper. Whether the Partner Center submission API is worth using later is left open.
- **Version `1.0.0` (settled):** it had no tag; the maintainer identified commit `1cc5ebc` (2026-11-02, "Archive store-packaging and add the Store listing texts", `Directory.Build.props` at `1.0.0`) as the `1.0.0` release, the version submitted to the Store, and the `v1.0.0` tag was created on it. The changelog entry for `1.0.0` summarizes everything built up to that commit, and `1.1.0` starts right after it.
- Which exact Store length limit applies to "What's new" must be confirmed in Partner Center when implementing.

## Risks / Trade-offs

- A changelog entry can be forgotten; the rule in `AGENTS.md` and the release checklist mitigate it.
- Reconstructing 3 entries from specs takes some editing work, and wording will need the maintainer's review.
