# README sync report

최종 업데이트: 2026-07-29 (KST)

## Current Ground Truth

| 항목 | 실제값 | 검증 |
|------|--------|------|
| CLI entrypoint count | **178** | published CLI `--help` + registration guardrail |
| Machine catalog count | **170** | published CLI `schema --format json` / `tools --json` parity smoke |
| MCP tool count | **12** | README + MCP black-box tests |
| PR .NET xUnit test inventory | **940** | Shared/Core/Cli/Mcp local Release test output |

## Synced Public Docs

| 위치 | 현재값 | 상태 |
|------|--------|------|
| `README.md` hero / comparison / command heading / architecture | 178 commands, 940 PR .NET tests | ✅ |
| `README.ko.md` hero / comparison / command heading / architecture | 178 명령, 940 PR .NET 테스트 | ✅ |
| `README.md` / `README.ko.md` agent skill install | Claude Code + Codex 공용 `unityctl-workflows` | ✅ |
| `docs/assets/tools.svg` README-rendered command summary | 178 commands, 12 MCP tools | ✅ |
| `docs/assets/token-efficiency.svg` README-linked command summary | 178 commands | ✅ |
| `docs/ref/architecture-mermaid.md` architecture block | 940 PR .NET xUnit tests | ✅ |
| `docs/ref/getting-started.md` architecture block | 940 PR .NET xUnit tests | ✅ |
| `docs/ref/ai-quickstart.md` machine-readable schema note | 170 canonical commands | ✅ |

## CI Guardrails

- `.github/workflows/ci-dotnet.yml` validates published CLI `schema` and `tools --json` parse successfully, expose matching command names, include `doctor`, `check`, `workflow-verify`, `scene-snapshot`, `scene-diff`, and `player-settings`, and execute `doctor --json` against a mini Unity project.
- `.github/workflows/ci-dotnet.yml` packs the current PR CLI nupkg, installs that exact version with `dotnet tool install --tool-path`, verifies installed-tool `schema` / `tools --json` command-name parity plus required README commands, and smokes `doctor --json`, `check --json`, and `workflow verify --json` JSON contracts.
- `.github/workflows/ci-unity.yml` runs manual/nightly smoke for mini-project `init`, sample-project `doctor`, `check`, representative read `scene hierarchy`, representative write/readback `player-settings set/get`, and `workflow verify`; validates live JSON success/readback evidence; then uploads the artifacts. The Unity version matrix uses `fail-fast: false` so one Unity version cannot cancel evidence collection for the other, and a preflight step writes `license-preflight.txt` plus `planned-smoke.txt` before requiring either `UNITY_LICENSE` or `UNITY_SERIAL`.
- CI/release workflows use Node 24-ready action majors for `checkout`, `setup-dotnet`, artifact upload/download, and GitHub Release creation.
- `.github/workflows/release.yml` runs Shared/Core/Cli/Mcp tests as a hard gate before packaging, NuGet publish, and GitHub Release creation.

Remote CI evidence is tracked on PR #16 so each new documentation commit can point reviewers at the current head instead of pinning this status report to a stale run id. The previous macOS timeout race in `AsyncCommandRunnerFlightTests.Timeout_ReturnsTestFailedResponse` is covered by the stabilized async timeout path, and the Windows published/tool smoke path now executes through `ProcessStartInfo` to avoid PowerShell native command exit-code drift.

Unity live evidence is tracked on issue #17 and PR #16. Current preflight evidence shows the workflow is blocked by missing `UNITY_LICENSE` / `UNITY_SERIAL`, while `license-preflight.txt` and `planned-smoke.txt` artifacts preserve intended coverage for `init`, `doctor`, `check`, `scene hierarchy`, `player-settings set/get`, and `workflow verify`.

Unity live blocker tracking issue: #17 (`Configure Unity Integration Actions secret`). Current `gh secret list --repo Jason-hub-star/unityctl` evidence shows only `NUGET_API_KEY`; add `UNITY_LICENSE` or `UNITY_SERIAL`, rerun `ci-unity.yml`, and download artifacts before closing the blocker.

## Local Verification Evidence

2026-07-29 local verification:

| Gate | Result |
|------|--------|
| `dotnet restore` | ✅ |
| `dotnet build --no-restore -c Release` | ✅ warning 0 / error 0 |
| `dotnet test tests/Unityctl.Shared.Tests --no-build -c Release` | ✅ 112 passed |
| `dotnet test tests/Unityctl.Core.Tests --no-build -c Release` | ✅ 177 passed |
| `dotnet test tests/Unityctl.Cli.Tests --no-build -c Release` | ✅ 626 passed |
| `dotnet test tests/Unityctl.Mcp.Tests --no-build -c Release` | ✅ 25 passed |
| published CLI `schema` / `tools --json` / `doctor --json` smoke | ✅ 170 canonical commands, no drift, doctor JSON shape valid |
| local nupkg `dotnet tool install --tool-path` smoke | ✅ installs the current PR `unityctl` package; schema/tools parity, required README commands, doctor/check/workflow verify JSON shape smoke passed |
| local `init --source src/Unityctl.Plugin` smoke | ✅ mini project manifest/settings written |

Unity Editor-dependent smoke in `.github/workflows/ci-unity.yml` still requires the GitHub Actions Unity environment and either a `UNITY_LICENSE` or `UNITY_SERIAL` secret to prove the live Editor portions (`check`, `scene hierarchy`, `player-settings set/get` with value readback, `workflow verify`) end-to-end. If the secret is missing, `license-preflight.txt` and `planned-smoke.txt` artifacts preserve the failure reason and intended live coverage.
