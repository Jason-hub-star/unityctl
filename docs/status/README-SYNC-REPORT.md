# README sync report

최종 업데이트: 2026-06-02 (KST)

## Current Ground Truth

| 항목 | 실제값 | 검증 |
|------|--------|------|
| CLI command count | **166** | published CLI `schema --format json` / `tools --json` smoke |
| MCP tool count | **12** | README + MCP black-box tests |
| PR .NET xUnit test inventory | **835** | Shared/Core/Cli/Mcp local Release test output |

## Synced Public Docs

| 위치 | 현재값 | 상태 |
|------|--------|------|
| `README.md` hero / comparison / command heading / architecture | 166 commands, 835 PR .NET tests | ✅ |
| `README.ko.md` hero / comparison / command heading / architecture | 166 명령, 835 PR .NET 테스트 | ✅ |
| `docs/assets/tools.svg` README-rendered command summary | 166 commands, 12 MCP tools | ✅ |
| `docs/assets/token-efficiency.svg` README-linked command summary | 166 commands | ✅ |
| `docs/ref/architecture-mermaid.md` architecture block | 835 PR .NET xUnit tests | ✅ |
| `docs/ref/getting-started.md` architecture block | 835 PR .NET xUnit tests | ✅ |
| `docs/ref/ai-quickstart.md` machine-readable schema note | 166 commands | ✅ |

## CI Guardrails

- `.github/workflows/ci-dotnet.yml` validates published CLI `schema` and `tools --json` parse successfully, expose matching command names, include `doctor`, `check`, `workflow-verify`, `scene-snapshot`, `scene-diff`, and `player-settings`, and execute `doctor --json` against a mini Unity project.
- `.github/workflows/ci-dotnet.yml` packs the current PR CLI nupkg, installs that exact version with `dotnet tool install --tool-path`, and smokes `schema`, `tools --json`, `doctor --json`, `check --json`, and `workflow verify --json` JSON contracts.
- `.github/workflows/ci-unity.yml` runs manual/nightly smoke for mini-project `init`, sample-project `doctor`, `check`, representative read `scene hierarchy`, representative write/readback `player-settings set/get`, and `workflow verify`; validates live JSON success/readback evidence; then uploads the artifacts.
- CI/release workflows use Node 24-ready action majors for `checkout`, `setup-dotnet`, artifact upload/download, and GitHub Release creation.
- `.github/workflows/release.yml` runs Shared/Core/Cli/Mcp tests as a hard gate before packaging, NuGet publish, and GitHub Release creation.

Remote CI note: the latest checked `CI - dotnet` failure on `master` (run `24076930620`, 2026-04-07) failed on Ubuntu/macOS in `StatusCommandTests.SmartWait_LockedThenUnlocked_StopsEarly` and `StatusCommandTests.SmartWait_LockedThenIpcReady_WaitsAndSucceeds` because the test path fell through when no interactive Unity process was detected. The current local guard injects the interactive-editor and delay dependencies explicitly, and the local Release CLI suite now passes 574/574.

## Local Verification Evidence

2026-06-02 local reproduction:

| Gate | Result |
|------|--------|
| `dotnet restore` | ✅ |
| `dotnet build --no-restore -c Release` | ✅ warning 0 / error 0 |
| `dotnet test tests/Unityctl.Shared.Tests --no-build -c Release` | ✅ 89 passed |
| `dotnet test tests/Unityctl.Core.Tests --no-build -c Release` | ✅ 146 passed |
| `dotnet test tests/Unityctl.Cli.Tests --no-build -c Release` | ✅ 578 passed |
| `dotnet test tests/Unityctl.Mcp.Tests --no-build -c Release` | ✅ 22 passed |
| published CLI `schema` / `tools --json` / `doctor --json` smoke | ✅ 166 commands, no drift, doctor JSON shape valid |
| local nupkg `dotnet tool install --tool-path` smoke | ✅ installed current `unityctl 0.3.2`; schema/tools/doctor/check/workflow verify smoke passed |
| local `init --source src/Unityctl.Plugin` smoke | ✅ mini project manifest/settings written |

Unity Editor-dependent smoke in `.github/workflows/ci-unity.yml` still requires the GitHub Actions Unity environment and `UNITY_LICENSE` secret to prove the live Editor portions (`check`, `scene hierarchy`, `player-settings set/get` with value readback, `workflow verify`) end-to-end.
