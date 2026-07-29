# Competitive analysis — 2026-07-29

공개 저장소와 공식 문서를 2026-07-29 KST에 다시 확인한 스냅샷이다. 별 개수나 도구 개수만으로 우열을 판정하지 않고, 설치·발견성·실행 신뢰성·검증 증거·토큰 비용을 제품 판단 기준으로 사용한다.

## 비교 대상

| 프로젝트 | 확인 버전/상태 | 강점 | unityctl에 주는 신호 |
|---|---|---|---|
| [CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp) | v10.1.0, 약 12.9k stars | 큰 사용자 기반, multi-instance routing, remote auth, Asset Store/OpenUPM 설치, AI asset/audio 생성 | 배포 채널과 onboarding UX가 기능만큼 중요 |
| [IvanMurzak/Unity-MCP](https://github.com/IvanMurzak/Unity-MCP) | v0.86.3, 약 3.7k stars | Unity/OS/package 기반 skill 생성, Editor+Runtime, stdio+HTTP/cloud, custom tool 확장 | agent skill과 runtime extensibility가 빠르게 표준화됨 |
| [CoderGamester/mcp-unity](https://github.com/CoderGamester/mcp-unity) | v1.4.0, 약 1.85k stars | MCP App dashboard, project-local Claude/Codex/Cursor 설정, bounded response, private serialized field 처리 | 팀 공유 설정과 interactive evidence UI가 새 경쟁면 |
| [akiojin/unity-cli](https://github.com/akiojin/unity-cli) | v0.12.0, 130 tool catalog | 단일 Rust binary, 14개 on-demand skills, local C# navigation/LSP, Input System, Addressables, video capture, auto-update | CLI-first + skill-first 조합이 직접 경쟁 축으로 성장 |
| [Unity-Technologies/skills](https://github.com/Unity-Technologies/skills) | main, 2026-07-28 최신 변경 | 공식 `unity-cli`/project/package/live-game/IAP skills, `npx skills add` 설치 | 공식 CLI 자체보다 공식 agent workflow가 adoption 기준을 바꿈 |
| [Unity CLI](https://docs.unity.com/en-us/unity-cli) | 1.0.0-beta.2, experimental | Editor/project/module/license/CI lifecycle, Pipeline live command/eval, 공식 지원 | lifecycle은 연동하고 Editor control의 안전·검증 계층으로 차별화 |

GitHub 수치는 조사 시점의 공개 API 스냅샷이며 시간에 따라 변한다.

## 현재 판단

unityctl의 방어 가능한 우위는 178개 CLI 명령 자체가 아니다.

- 12개 MCP 도구로 178개 명령을 on-demand schema로 노출하는 낮은 상시 토큰 비용
- IPC probe-first + batch fallback, reload-aware reconnect, 무인 Editor update dispatch
- Undo rollback, dry-run, flight log, sessions, `workflow verify` evidence bundle
- 공식 Unity CLI와 같은 Editor/태스크에서 측정한 실패 의미론·응답 크기·지연 증거

반대로 공개 경쟁자는 다음 영역에서 앞선다.

- agent skill 배포와 project-local Claude/Codex 설정
- 자동 업데이트와 one-shot onboarding
- local C# semantic navigation/LSP
- Input System simulation, Addressables, video capture
- dashboard/MCP App 기반 사람 검토 경험

## 우선순위

| 순위 | 개선 | 이유 | keep 기준 |
|---|---|---|---|
| P0 | 신뢰성 회귀 제거 | 기능 추가보다 기존 공개 계약의 거짓 음성이 치명적 | 실제 Unity 프로젝트 재현 + 수정 후 readback + .NET 회귀 0 |
| P0 | project-local agent onboarding | CoderGamester/Ivan/akiojin의 설치 후 첫 성공 시간이 짧음 | Claude/Codex에서 저장소 공유 가능한 설정/skill 설치 smoke |
| P0 | 멀티 인스턴스 Phase 2 | macOS/Linux process inventory가 true editor identity의 기반 | GUI Editor와 worker/batch를 OS별로 구분 |
| P1 | workflow skills | 공식 Unity Skills와 CLI-first 경쟁자의 핵심 발견성 | raw catalog를 복제하지 않는 5개 이하 고가치 workflow |
| P1 | local code intelligence | Editor 연결 없이 가능한 빠른 루프 | 기존 stdlib/설치 dependency 재사용 여부를 먼저 검토 |
| P1 | Input/Addressables/video | 실제 게임 검증 루프를 넓힘 | 실험 프로젝트에서 end-to-end evidence가 있을 때만 keep |
| P2 | dashboard/MCP App | adoption에는 도움되지만 core control plane 필수는 아님 | CLI evidence보다 실제 사용자 시간이 줄 때만 추진 |

## 1차 실험 결과 — keep

별도 `/Users/family/jason/unityctl-lab` 프로젝트를 공식 Unity CLI로 Unity 6000.3.16f1에 생성하고 로컬 bridge를 설치했다.

1. `component get --full`
   - baseline: `m_Constraints=80`을 개별 조회할 수 있지만 full dump에서 누락
   - root cause: 공용 유틸이 `SerializedProperty.NextVisible`만 순회
   - result: hidden top-level serialized state까지 순회하되 child 중복은 피함
   - live readback: `m_Constraints: 80`
2. macOS process inventory
   - baseline: IPC 연결 상태에서도 `target.isRunning=false`; compile 중 `await-ready` 즉시 실패
   - root cause: `MacOsPlatform.FindRunningUnityProcesses()` 미구현
   - result: `/bin/ps`를 읽어 GUI Editor와 `-adb2/-batchMode` worker 분류
   - live readback: PID, `interactive`, `isRunning=true`; reload 뒤 2.47초 내 Ready
3. registry metadata
   - baseline: `server.json`이 v0.3.0, 이전 GitHub identity, 존재하지 않는 npm 배포를 선언
   - result: NuGet `unityctl-mcp`, v0.6.1, 현재 GitHub identity로 동기화하고 회귀 가드 추가

## 2차 실험 결과 — keep

`skill-creator`로 단일 `unityctl-workflows` 스킬을 만들고 별도 에이전트가
문서만 읽은 상태에서 `unityctl-lab`을 진단하게 했다.

- 로컬 `npx skills add`가 공용 `.agents/skills` 설치와 Claude Code symlink를 생성
- 첫 전방 테스트가 dirty scene, 전역 target mismatch, 구형 CLI의 macOS process
  false negative를 모두 쓰기 blocker로 판단
- 잘못된 `editor current --project` 예시, dirty guard, tools JSON shape 등 6개
  문서 결함을 수정
- 두 번째 전방 테스트에서 6개 결함 해소 및 0.6.1 process metadata 정상화 확인
- project-local config 자동 생성은 아직 추가하지 않음. 공용 스킬 설치로 같은
  목적을 달성하며, 설정 파일 생성은 실제 사용성 데이터가 요구할 때만 검토

## 다음 실험

OS process inventory의 남은 Linux 공백을 실제 또는 격리된 process fixture로
재현한다. 동시에 NuGet package readme 경고처럼 설치 후 첫 성공을 방해하는
배포 마찰을 새 기능보다 먼저 제거한다.
