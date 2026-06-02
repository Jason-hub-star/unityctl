## Summary

- 

## Test Trust Checklist

- [ ] PR .NET gate stays green: Shared/Core/Cli/Mcp on Linux, macOS, and Windows.
- [ ] Local focused tests were run for the changed layer(s).
- [ ] No flaky test is left as "sometimes fails"; file `.github/ISSUE_TEMPLATE/flaky-test.yml` if isolation is still needed.
- [ ] Bug fixes include a failing reproduction test, or link a `.github/ISSUE_TEMPLATE/regression-bug.yml` issue that explains the missing coverage.

## Contract Safety Checklist

For new or changed commands:

- [ ] `WellKnownCommands` is updated in Shared and Plugin shared copy when needed.
- [ ] `CommandCatalog` and schema/tool metadata are updated.
- [ ] CLI registration in `src/Unityctl.Cli/Program.cs` is updated.
- [ ] MCP `QueryTool`/`RunTool` allowlist/schema coverage is updated.
- [ ] Plugin handler registration/coverage is updated.
- [ ] CLI verb and Plugin handler command names do not duplicate or shadow another command.
- [ ] `CommandSyncGuardrailTests` pass.

## README User Path

- [ ] Published CLI smoke remains covered: `dotnet tool install`, `unityctl tools --json`, `unityctl schema`, `doctor`, `check`, and `workflow verify`.
- [ ] README/README.ko/docs describe any public surface or validation-scope change.

## Unity Reality Check

- [ ] PR intentionally uses fast .NET tests only, or Unity Integration was run manually/nightly.
- [ ] If Unity Integration was run, artifacts were uploaded for the sample project validation.
- [ ] If Unity Integration could not run, note whether `UNITY_LICENSE` or `UNITY_SERIAL` is missing and attach/check `license-preflight.txt` plus `planned-smoke.txt`.
