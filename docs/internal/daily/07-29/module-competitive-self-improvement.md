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
5. Claude Code/Codex 공용 `unityctl-workflows` 스킬과 설치 경로 추가
6. CLI/MCP NuGet package에 기존 루트 README 포함
7. Linux `/proc` process inventory와 실제 container probe 추가
8. CLI 178 entrypoints / machine catalog 170개 문서 의미 분리
9. IPC listener publish/stop race를 공용 lock에서 제거
10. connect-only readiness probe를 1초 bounded `ping` roundtrip으로 교체

## 검증

- 수정 전 `component get --full`: `m_Constraints` 누락
- 수정 후 live readback: `m_Constraints: 80`
- 수정 전 target metadata: `isRunning=false`
- 수정 후: PID 93832, `processKind=interactive`, `isRunning=true`
- reload 뒤 `await-ready`: 2.47초 내 Ready
- `dotnet build unityctl.slnx -c Release -m:1 /p:UseSharedCompilation=false`: 경고 0 / 오류 0
- 전체 솔루션: 961개 통과(Shared 110, Core 177, Cli 626, MCP 25, Integration 23)
- v0.6.1 CLI/MCP NuGet pack + local tool install + 178-command schema/tools parity smoke 통과
- MCP Registry `server.json` 공식 2025-12-11 JSON Schema 검증 통과
- `unityctl-workflows` 스킬 정적 검증 + 로컬 Claude Code/Codex 설치 smoke 통과
- 2회 전방 테스트: 1차에서 명령/dirty/routing/process 판정 결함 6개 발견,
  수정 후 2차에서 전부 해소 확인
- 공개 GitHub source에서 Claude Code/Codex skill 설치 smoke 통과
- NuGet.org CLI/MCP v0.6.1 공개 인덱싱 확인
- CLI/MCP pack 경고 0, 두 `.nupkg`의 nuspec `<readme>README.md</readme>`와
  root `README.md` entry 확인
- Linux SDK container: `LinuxPlatformTests` 9개 통과, 실제 `/proc` probe 포함
- v0.6.2 local CLI/MCP install, version 0.6.2, schema/tools 170개 parity 통과
- Unity lab domain reload 1.5초 내 완료와 IPC 재기동 확인
- 실제 window close 뒤 Unity PID·`Temp/UnityLockfile` 제거 확인
- 새 CLI status 10회: expected pipe-close warning delta 0
- bounded ping probe `await-ready`: 1회, 602ms, Ready

## 다음 후보

- project-local Claude/Codex config 생성은 스킬 사용성 데이터가 필요할 때만 추가
- Windows quit-hang #12/#13 master 재검증 요청
- clean-project 공개 설치 첫 성공 시간 smoke
