# unityctl 전체 Phase 로드맵

> 최종 업데이트: 2026-03-18
> 목표: MCP 상위 호환 Unity 제어 체인

---

## MCP 대체 매핑

| MCP 기능 | unityctl 대응 | Phase | 상태 |
|----------|-------------|-------|------|
| Tools | CLI 커맨드 + `--json` | 0 ~ 2A+ | ✅ |
| tools/list | `unityctl tools --json` | 2A+ | ✅ |
| Resources | flight log, scene snapshot | 3B, 4B | 🔲 |
| Prompts | `docs/ref/ai-quickstart.md` | 1C | ✅ |
| Tasks | Session Layer | 3A | 🔲 |
| Streaming | Watch Mode | 3C | 🔲 |

unityctl은 MCP를 대체하는 동시에, 필요하면 나중에 얇은 MCP bridge를 덧씌우는 방향을 유지합니다.

---

## Phase 구조

```text
Phase 0   — 프로젝트 골격         ✅ 완료
Phase 0.5 — Plugin 부트스트랩     ✅ 완료
Phase 1A  — CLI 기본              ✅ 완료
Phase 1B  — 핵심 기능             ✅ 완료
Phase 1C  — 테스트 + 배포         ⚠️ 부분 완료
Phase 2A  — Foundation            ✅ 완료
Phase 2A+ — Tools Metadata        ✅ 완료
Phase 2B  — IPC Transport         ✅ 완료
Phase 2C  — Async Commands        ✅ 완료
Phase 3A  — Session Layer         🔲 미착수
Phase 3B  — Flight Recorder       🔲 미착수
Phase 3C  — Watch Mode            🔲 미착수
Phase 4A  — Ghost Mode            🔲 미착수
Phase 4B  — Scene Diff            🔲 미착수
Phase 5   — Agent Layer           🔲 미착수
```

---

## Phase 2B — IPC Transport

상태: **구현 완료, 후속 보강 필요**

현재 코드에 반영된 것:

- Plugin `IpcServer`
- Core `IpcTransport`
- `PipeNameHelper`
- 양쪽 `MessageFraming`
- `UnityctlBootstrap` IPC 서버 시작
- `CommandExecutor` probe-first
- 3개 플랫폼 `CreateIpcClientStream`
- IPC 관련 Core 테스트

실제 검증된 범위:

- `dotnet build unityctl.slnx` 통과
- `dotnet test unityctl.slnx` 통과
- `robotapp`에서 열린 Editor 기준 `status` 성공
- `robotapp`에서 열린 Editor 기준 `ping` 성공
- `robotapp`에서 열린 Editor 기준 `check` 성공
- `robotapp`에서 열린 Editor 기준 `test --mode edit`가 비동기 시작 의미로 `Busy` 반환
- `robotapp`에서 열린 Editor 기준 `build` 요청이 실제 `BuildHandler`까지 도달함을 확인
- Unity 재시작 후 IPC가 다시 `ping/status`로 회복됨을 확인
- Unity 미실행 상태에서 batch fallback 동작 확인

아직 남은 후속 보강:

- 도메인 리로드 후 IPC 자동 복구에 대한 더 강한 재현/종결 검증
- batch worker에서 IPC 서버 미기동 로그 검증
- pure IPC latency를 CLI 프로세스 시작 비용과 분리한 추가 측정

상세 내용은 `docs/ref/phase-2b-plan.md`와 `docs/DEVELOPMENT.md`를 함께 봅니다.

### 2B 리스크 메모

| 리스크 | 상태 | 메모 |
|--------|------|------|
| 서버 종료 시 pending work 정리 | ✅ | shutdown completion 기반으로 보강 |
| 열린 Editor build 성공이 프로젝트 상태에 의존 | ⚠️ | transport는 검증됐지만 `robotapp` 컴파일 에러로 build 자체는 실패 |
| domain reload 자동 복구 종결 검증 미흡 | ⚠️ | 재시작 복구는 확인했지만 reload-only 자동 회복은 더 확인 필요 |
| pure latency 측정 부재 | ⚠️ | built exe 기준 warmed state는 확인했지만 transport-only 수치는 아님 |

---

## Phase 2C — Async Commands

상태: **구현 완료**

구현된 것:

- `AsyncOperationRegistry` — single-flight guard + age-check (360s) + TTL prune (Running 10분, Completed 5분)
- `TestResultCollector` — `ICallbacks` + `IErrorCallbacks` 동시 구현, leaf-only 집계 (`HasChildren` 필터)
- `TestResultHandler` — `test-result` IPC 내부 커맨드, 멱등 polling 응답
- `AsyncCommandRunner` — CLI delegate 주입 폴링 (500ms 초기 → 1s 간격)
- `TestCommand` — `--no-wait` (ConsoleAppFramework flag), `--timeout` (기본 300s)
- PlayMode + wait → 경고 + no-wait 강제
- `ConsoleOutput` — `ACCEPTED [104]` Cyan 출력 분기
- `UnityctlBatchEntry` — Accepted 감지 → `EditorApplication.update` 폴링 대기 (300s)
- `UnityctlBootstrap` — 60초 주기 `AsyncOperationRegistry.Prune` 훅
- `StatusCode.Accepted = 104`, `WellKnownCommands.TestResult = "test-result"`
- `CommandCatalog.Test`에 `wait`, `timeout` 파라미터 추가

실제 검증 (robotapp, 2026-03-18):

- `test` 기본 모드: 폴링 후 404 passed, 27.7s 소요
- `test --no-wait`: `ACCEPTED [104]` 즉시 반환
- `test --mode play`: 경고 + 즉시 반환
- `test --timeout 5`: 타임아웃 후 `TestFailed`
- single-flight: 두 번째 요청 `Busy`
- 85개 dotnet 테스트 통과

---

## Phase 3A — Session Layer

MCP `Tasks` 대응용 내부 세션 추상화.

방향:

- `SessionManager`
- `SessionState`
- `SessionStore`

MCP Tasks는 experimental이므로 내부 세션 모델을 먼저 만들고, MCP 노출은 후행 매핑으로 둡니다.

---

## Phase 3B — Flight Recorder

모든 커맨드 실행을 구조화된 로그로 남기는 단계.

예상 산출물:

- `FlightLog`
- `FlightEntry`
- `unityctl log`

---

## Phase 3C — Watch Mode

Unity 이벤트를 스트림으로 전달하는 단계.

예상 범위:

- console
- hierarchy
- compile

공식 문서 기준 주의사항:

- `Application.logMessageReceivedThreaded`는 병렬 호출 가능
- `EditorApplication.hierarchyChanged`는 다음 editor update에서 발생
- `CompilationPipeline` start/finish는 context로 매칭 가능

---

## Phase 4A — Ghost Mode

범용 dry-run API가 아니라 preflight validation으로 정의하는 편이 안전합니다.

예상 범위:

- target/path/scene 검증
- 위험 요소 보고

---

## Phase 4B — Scene Diff

외부 YAML 파싱보다 Unity `SerializedObject` API 활용이 우선입니다.

예상 범위:

- scene snapshot
- propertyPath 기반 diff
- multi-scene 고려

---

## Phase 5 — Agent Layer

핵심 원칙:

- unityctl은 primitive 제공
- orchestration은 AI agent에 위임
- 필요 시 JSON workflow와 MCP bridge를 선택적으로 제공

---

## 다음 우선순위

1. Phase 2B 후속 보강 (domain reload, batch IPC 미기동 로그, latency 측정)
2. Phase 3A Session Layer
3. Phase 1C release/README
