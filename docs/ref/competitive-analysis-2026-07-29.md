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

- 178개 CLI 진입점과 별도로 정규화된 170개 command schema를 12개 MCP
  도구에서 on-demand로 불러오는 낮은 상시 토큰 비용
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

## 3차 실험 결과 — keep

- Linux의 빈 `FindRunningUnityProcesses()`를 `/proc/<pid>/exe`와 NUL 구분
  `cmdline` 읽기로 구현
- 공백 포함 `-projectPath`, Unity Hub version, `-batchmode`/`-nographics`/
  `-adb2` 분류를 parser fixture로 검증
- Linux .NET SDK container에서 실제 `Unity` probe process를 띄워 PID,
  project, version, interactive classification을 `/proc` 경로로 검증
- 릴리스 pack 경고에서 발견한 CLI/MCP NuGet README 누락도 기존 루트 README
  재사용으로 제거
- package smoke 과정에서 CLI 178 entrypoints와 machine catalog 170개를
  혼용한 문서 오류를 발견해 분리 표기하고 회귀 가드 추가

## 4차 실험 결과 — master 후보

공개 이슈 #12/#13의 root-cause 설명과 현재 `IpcServer`를 대조했다. quit 시
긴 thread join을 생략하는 기존 fast path는 있었지만, listener가 pipe 생성과
`_listenPipe` 게시 사이에서 종료되면 새 pipe를 놓치는 race는 남아 있었다.

- `StopInternal`과 같은 lock 안에서 `_stopping` 확인과 `_listenPipe` 게시를
  원자화해 dispose 누락 창을 제거
- exact source-shape guardrail 추가
- Unity 6000.3.16f1 lab domain reload 1.5초 내 완료, IPC 재기동 확인
- 저장 후 실제 window close에서 Unity PID와 `Temp/UnityLockfile` 제거 확인

원 보고 환경은 Windows 11이므로 master 후보를 공개한 뒤 reporter 재검증 전에는
이슈를 닫거나 릴리스 완료로 선언하지 않는다.

## 5차 실험 결과 — keep

readiness probe가 pipe 연결 성공 직후 payload 없이 닫아 서버의
`MessageFraming.ReadMessage`가 정상적으로 `EndOfStreamException`을 내고, 이를
실제 IPC 오류처럼 로그에 남기는 것이 원인이었다.

- connect-only probe를 `ping` request/response roundtrip으로 교체
- 사용자 cancellation은 계속 전파하고, probe 자체는 1초 예산으로 제한
- named-pipe test가 `ping` payload와 성공 response를 검증
- Unity lab에서 새 CLI로 status 10회 실행: expected pipe-close warning delta 0
- `await-ready`: 1회, 602ms, Ready

## 6차 실험 결과 — keep

공식 Unity CLI로 별도 `unityctl-onboarding-lab`을 만들고 공개 v0.6.2만
사용해 소비자 경로를 측정했다.

- project create + public skill install + embedded bridge init 묶음: 14.7초
- Editor 기동 후 `await-ready`: 명령 416ms, 내부 329ms, 1회
- `doctor`: 365ms, embedded bridge + healthy IPC
- scene create → GameObject ID readback → save: 2.998초
- 첫 validate가 build scene 미등록을 정확히 발견했지만
  `data.valid=false`와 동시에 `success=true/statusCode=0`을 반환하는 계약
  버그도 노출
- 공용 `ProjectValidateHandler`를 `TestFailed(504)`로 수정한 contributor
  lab 재검증: `success=false`, CLI exit 1
- build scene 등록 → validate green: 0.940초, 6/6 통과
- skill recipe에 v0.6.2 이하 `data.valid` 호환 판정과 복구 명령 추가

새 하네스나 명령은 필요하지 않았다. 기존 CLI, 공식 Unity CLI, 공개 skills
installer 조합만으로 측정과 복구가 가능했다.

## v0.6.3 릴리스 판단

최종 master CI가 Ubuntu 1m31s, macOS 1m35s, Windows 2m31s에 모두
통과했고 각 OS published CLI/local tool smoke도 green이었다. reporter가
로컬 source 설치 없이 검증할 수 있도록 세 hardening을 v0.6.3 패치로
릴리스한다. 단, Windows Unity 라이선스 증거는 아니므로 #12/#13은 닫지 않고
종료 fix 증거를 “logical root fix + macOS Unity live + Windows .NET CI”로
한정한다.

## 7차 실험 결과 — keep

local code intelligence를 새 LSP로 재구현하기 전에 기존 `script find-refs`의
정확성을 Unity lab에서 점검했다. 기존 handler는 줄마다 첫 번째 단어 경계만
반환해 한 줄에 같은 심볼이 여러 번 등장하면 reference count와 column이
누락됐다.

- 새 의존성이나 계층 없이 같은 줄의 다음 검색 위치를 계속 순회
- Unity 6000.3.16f1 lab probe에서 `transform` 5개를 반환
- 같은 9행의 두 occurrence를 column 20과 41로 각각 readback
- Shared source guardrail 포함 전체 963개 .NET 테스트 통과

semantic 구분은 여전히 경쟁 LSP의 우위이며 결과는 comments/strings를 포함할
수 있다. 다음 단계는 이 한계를 숨기지 않고, Editor 연결 없는 탐색이 실제
시간을 줄이는지 먼저 벤치마크한다.

## 8차 실험 결과 — keep

`script find-refs`의 word-boundary 계약은 Roslyn/LSP가 없어도 로컬 파일에서
동일하게 수행할 수 있으므로 Core scanner로 이동했다. `CommandExecutor`의
공용 진입점에서 처리해 CLI와 MCP가 같은 경로를 사용하며, 세션과 flight log
기록도 유지한다.

- running lab: 동일 `transform` 5개와 두 column readback 유지
- locked Editor fixture: IPC probe 없이 local response를 반환하는 회귀 테스트
- closed sample project: 공개 v0.6.2 batch Unity 4.34초 → local 0.21초
  (**20.7배**, 결과 1개 동일)
- project 밖 folder 거부, deterministic file ordering, 실제 추가 match가 있을
  때만 `truncated=true`
- 전체 967개 .NET 테스트와 warning 0 build 통과

이는 semantic navigation 전체를 복제한 것이 아니다. 현재 명령의 정직한
text-search 계약을 더 빠르고 독립적으로 만든 슬라이스이며, overload 구분이나
comments/strings 제외가 필요하다는 실제 사례가 생길 때 Roslyn 비용을 다시
평가한다.

## 다음 실험

v0.6.3 CLI/MCP NuGet 인덱싱과 글로벌 tool update, schema/tools 170개 parity
smoke까지 완료했다. 사용자 요청으로 자기개선 루프를 여기서 종료한다.
Input System/Addressables/video 비교는 후속 backlog로 남기며, Windows
확인이 없으므로 #12/#13은 억지로 닫지 않는다.
