# Contributing to unityctl

Thanks for helping make `unityctl` more reliable. This project treats tests as part of the public API: contributors should keep fast PR checks green while preserving separate Unity Editor evidence for live validation.

## Pull request baseline

Every PR should keep the .NET gate green on Linux, macOS, and Windows:

```bash
dotnet test tests/Unityctl.Shared.Tests -c Release
dotnet test tests/Unityctl.Core.Tests -c Release
dotnet test tests/Unityctl.Cli.Tests -c Release
dotnet test tests/Unityctl.Mcp.Tests -c Release
```

For focused changes, run the smallest relevant filter locally first, then rely on CI for the full three-OS matrix. Do not leave a failing or flaky Shared/Core/Cli/Mcp test as "sometimes fails".

The PR workflow must keep its `pull_request` trigger for `main`/`master`, run the `ubuntu-latest`, `windows-latest`, and `macos-latest` matrix with `fail-fast: false`, and avoid `continue-on-error` for the .NET gate. `WorkflowGuardrailTests` watches those invariants so the public PR signal cannot silently shrink.

## Flaky and regression policy

- Flaky tests must be stabilized or isolated with evidence. Use `.github/ISSUE_TEMPLATE/flaky-test.yml` when a test needs follow-up isolation.
- Bug fixes should include a failing reproduction test in the same PR. Link a `.github/ISSUE_TEMPLATE/regression-bug.yml` issue when coverage cannot be added immediately.
- High-value regression areas include IPC timeout, AppLocker, batch fallback, dirty scene policy, parser edge case, and command/schema/plugin drift.
- Resolved date/time boundary regressions such as `FlightLogRobustnessTests.Query_FilterByUntil_ExcludesNewerEntries` should stay on fixed timestamps instead of drifting back to wall-clock assumptions.

## Adding or changing commands

New commands must stay synchronized across the public contract:

1. Update `WellKnownCommands` in Shared, and sync the Plugin shared copy when the command crosses the transport boundary.
2. Update `CommandCatalog`, schema/tool metadata, parameters, and examples.
3. Register the CLI verb in `src/Unityctl.Cli/Program.cs` and add parser/request tests.
4. Update MCP `QueryTool` or `RunTool` allowlist/schema coverage.
5. Add or update the Plugin handler under `src/Unityctl.Plugin/Editor/Commands`.
6. Confirm the CLI verb and Plugin handler command name are unique so no registration silently shadows another command.
7. Run `CommandCatalogTests`, `CommandSchemaTests`, and `CommandSyncGuardrailTests`.

`CommandSyncGuardrailTests` also protects against Plugin shared copy drift by comparing `WellKnownCommands`, wire DTO JSON fields, `StatusCode`, and Exec parser grammar sentinels between Shared and the Unity Plugin copy. It also fails duplicate CLI `app.Add(...)` or Plugin `CommandName` registrations before they can shadow a public command.

```bash
dotnet test tests/Unityctl.Shared.Tests -c Release --filter "CommandCatalogTests|CommandSchemaTests|CommandSyncGuardrailTests"
```

## README user path

The published CLI path should remain smoke-tested:

- `dotnet tool install`
- `unityctl tools --json`
- `unityctl schema`
- `doctor`
- `check`
- `workflow verify`

If a PR changes any public surface, update `README.md`, `README.ko.md`, and relevant docs in the same PR.

## Unity live validation

Regular PRs run fast .NET tests. Unity Editor-dependent validation lives in the Unity Integration workflow and covers sample-project `init`, `doctor`, `check`, representative read/write commands, and `workflow verify`.

Unity Integration requires either the `UNITY_LICENSE` or `UNITY_SERIAL` GitHub secret. When those secrets are unavailable, the workflow fails in a preflight step and uploads `license-preflight.txt` plus `planned-smoke.txt` artifacts instead of hiding the reason or intended live coverage inside GameCI logs.

To prove the live Unity gate after secrets are configured:

1. Add either `UNITY_LICENSE` or `UNITY_SERIAL` under repository Actions secrets.
2. Run `gh workflow run ci-unity.yml --ref <branch>`.
3. Watch it with `gh run watch <run-id> --exit-status`.
4. Download artifacts with `gh run download <run-id> --dir <artifact-dir>` and confirm both Unity versions include the sample-project command evidence.

If the run still stops at preflight, attach the downloaded `license-preflight.txt` and `planned-smoke.txt` artifacts to the PR notes so reviewers can see both the secret failure and the intended Unity live coverage.
