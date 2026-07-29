# 07-29 competitive self-improvement loop

## 조사

- 최신 공개 경쟁자: CoplayDev v10.1.0, IvanMurzak v0.86.3, CoderGamester v1.4.0, akiojin v0.12.0
- 공식 축: Unity CLI 1.0.0-beta.2, Unity-Technologies/skills
- 공용 Claude/Codex 하네스: `autoresearch-loop` 재사용, `scripts/check-harness.sh` 통과
- 상세 비교: `docs/ref/competitive-analysis-2026-07-29.md`

## 실험 프로젝트

- 경로: `/Users/family/jason/unityctl-lab`
- 생성: 공식 Unity CLI `projects create`
- Unity: 6000.3.16f1 arm64
- bridge: 저장소 `src/Unityctl.Plugin` file dependency
- fixture: `Assets/ConstraintProbe.unity`, `Rigidbody.m_Constraints=80`

## Keep

1. hidden top-level serialized property를 `component get --full`에 포함
2. macOS Unity process inventory 구현
3. MCP Registry metadata를 NuGet/v0.6.1/현재 repository로 동기화
4. version/test-count/public docs guardrail 동기화

## 검증

- 수정 전 `component get --full`: `m_Constraints` 누락
- 수정 후 live readback: `m_Constraints: 80`
- 수정 전 target metadata: `isRunning=false`
- 수정 후: PID 93832, `processKind=interactive`, `isRunning=true`
- reload 뒤 `await-ready`: 2.47초 내 Ready
- `dotnet build unityctl.slnx -c Release -m:1 /p:UseSharedCompilation=false`: 경고 0 / 오류 0
- 전체 솔루션: 954개 통과(Shared 110, Core 170, Cli 626, MCP 25, Integration 23)
- v0.6.1 CLI/MCP NuGet pack + local tool install + 178-command schema/tools parity smoke 통과
- MCP Registry `server.json` 공식 2025-12-11 JSON Schema 검증 통과

## 다음 후보

- project-local Claude/Codex onboarding + 얇은 workflow skills
- Linux process inventory
- Windows quit-hang 공개 issue의 v0.6.1 재검증과 issue 정리
