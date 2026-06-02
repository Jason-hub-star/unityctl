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

## Flaky and regression policy

- Flaky tests must be stabilized or isolated with evidence. Use `.github/ISSUE_TEMPLATE/flaky-test.yml` when a test needs follow-up isolation.
- Bug fixes should include a failing reproduction test in the same PR. Use `.github/ISSUE_TEMPLATE/regression-bug.yml` if coverage cannot be added immediately.
- High-value regression areas include IPC timeout, AppLocker, batch fallback, dirty scene policy, parser edge case, and command/schema/plugin drift.
- Date/time boundary tests such as `FlightLogRobustnessTests.Query_FilterByUntil_ExcludesNewerEntries` should use fixed timestamps instead of wall-clock assumptions.

## Adding or changing commands

New commands must stay synchronized across the public contract:

1. Update `WellKnownCommands` in Shared, and sync the Plugin shared copy when the command crosses the transport boundary.
2. Update `CommandCatalog`, schema/tool metadata, parameters, and examples.
3. Register the CLI verb in `src/Unityctl.Cli/Program.cs` and add parser/request tests.
4. Update MCP `QueryTool` or `RunTool` allowlist/schema coverage.
5. Add or update the Plugin handler under `src/Unityctl.Plugin/Editor/Commands`.
6. Run `CommandCatalogTests`, `CommandSchemaTests`, and `CommandSyncGuardrailTests`.

`CommandSyncGuardrailTests` also protects against Plugin shared copy drift by comparing `WellKnownCommands`, wire DTO JSON fields, `StatusCode`, and Exec parser grammar sentinels between Shared and the Unity Plugin copy.

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

Unity Integration requires either the `UNITY_LICENSE` or `UNITY_SERIAL` GitHub secret. When those secrets are unavailable, the workflow fails in a preflight step and uploads `license-preflight.txt` artifacts instead of hiding the reason inside GameCI logs.
