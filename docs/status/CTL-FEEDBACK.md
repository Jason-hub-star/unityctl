# CTL Feedback

## Summary

- Repeated issues and top improvements are summarized here before final submission.

## Entries

### 2026-03-25

- Phase: Phase 0
- Command: `unityctl package list`, `unityctl doctor`
- Pain Point: package metadata and cached resolution data could lag behind the manifest update, making version verification ambiguous until the Editor was restarted.
- Workaround: restart Unity and re-run `package list`, `doctor`, and inspect `Library/PackageManager/projectResolution.json`.
- Improvement Suggestion: add a dedicated `package resolve` or `doctor --packages` mode that reports manifest target, loaded package version, and stale cache mismatches in one place.
- Severity: medium

### 2026-03-25

- Phase: Phase 3
- Command: `unityctl scene open`, `unityctl ui find`
- Pain Point: after opening one scene, `ui find` could still surface UI from a previously active scene, which made scene-by-scene authored UI verification confusing.
- Workaround: rely on command return payloads, restart Unity when needed, and cross-check with build settings and saved scene paths.
- Improvement Suggestion: add an explicit active-scene assertion to `ui find` or a `--scene` filter for UGUI queries.
- Severity: high

### 2026-03-25

- Phase: Phase 3
- Command: `unityctl console get-entries`
- Pain Point: expected console subcommand naming was easy to guess wrong, and the fallback output was only the generic command list.
- Workaround: inspect `unityctl tools --json` or top-level help before retrying.
- Improvement Suggestion: improve unknown-command guidance with the nearest matching command name.
- Severity: low

### 2026-03-25

- Phase: Phase 6
- Command: `unityctl build`, `unityctl check`, `unityctl status --wait`
- Pain Point: immediately after asset refresh, build/check/status could fail with `103` while IPC was still reloading.
- Workaround: restart the Editor or wait for `Ready` before retrying build-related commands.
- Improvement Suggestion: expose a stronger `await-ready` command that blocks until IPC and compile state are both stable.
- Severity: medium

### 2026-03-25

- Phase: Phase 3
- Command: `unityctl screenshot capture --view game`
- Pain Point: Game View capture did not include the overlay UGUI authored on Screen Space - Overlay canvases, so visual UI verification via screenshot was misleading.
- Workaround: combine scene hierarchy/UI queries with manual in-editor inspection or change capture strategy instead of trusting the raw game screenshot.
- Improvement Suggestion: support overlay canvas capture in screenshot tooling or clearly document the limitation in the command help.
- Severity: high
