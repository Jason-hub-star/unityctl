# unityctl 개발 진행 상황

> 최종 업데이트: 2026-03-18

## 문서 구조

현재 `docs/`는 아래 구조를 기준으로 운영합니다.

- `docs/ref/` : 장기 참조 문서, 설계, 로드맵, 용어집
- `docs/status/` : 현재 phase 상태, 실행 보드, 프로젝트 상태
- `docs/daily/` : 일별 작업 기록
- `docs/weekly/` : 주간 요약
- `docs/DEVELOPMENT.md` : 현재 코드베이스 기준 단일 개발 현황 요약

새 문서를 추가하거나 기존 문서를 이동할 때는 루트가 아니라 위 분류에 맞춰 배치합니다.

---

## 프로젝트 개요

| 항목 | 값 |
|------|-----|
| 레포 위치 | `C:\Users\gmdqn\unityagent` |
| 런타임 | .NET 10 (SDK 10.0.201) |
| CLI 프레임워크 | ConsoleAppFramework 5.3.3 |
| Shared 타겟 | .NET Standard 2.1 |
| Core / CLI 타겟 | .NET 10 |
| Unity 플러그인 | UPM, Unity 2021.3+, Newtonsoft.Json 3.2.1 |
| 테스트 프레임워크 | xUnit 2.9.2 |
| 수동 검증 프로젝트 | `C:\Users\gmdqn\robotapp` (Unity 6000.0.64f1) |

주요 참조 문서:

- `docs/ref/getting-started.md`
- `docs/ref/ai-quickstart.md`
- `docs/ref/glossary.md`
- `docs/ref/phase-roadmap.md`
- `docs/ref/phase-2b-plan.md`
- `docs/status/PROJECT-STATUS.md`

---

## 아키텍처

```text
unityctl.slnx
├── src/Unityctl.Shared    (netstandard2.1)  프로토콜 + 모델 + 상수
├── src/Unityctl.Core      (net10.0)         비즈니스 로직 (transport, discovery, retry)
├── src/Unityctl.Cli       (net10.0)         얇은 CLI 셸
├── src/Unityctl.Plugin    (Unity UPM)       Editor 브릿지
├── tests/Unityctl.*.Tests xUnit 테스트
└── docs/                  ref/status/daily/weekly 구조
```

의존성 방향:

```text
Shared <- Core <- Cli
```

Plugin은 Unity 내부에서만 컴파일되며, `src/Unityctl.Plugin/Editor/Shared/`에 프로토콜 복사본을 유지합니다.

---

## Phase 현황

| Phase | 상태 | 요약 |
|-------|------|------|
| 0 ~ 1B | ✅ 완료 | 골격, Plugin, CLI 기본, 핵심 핸들러 |
| 1C | ⚠️ 부분 완료 | CI 있음, release/README 미완 |
| 2A / 2A+ | ✅ 완료 | Payload 정리, Core 추출, tools metadata |
| 2B | ✅ 완료 | IPC transport 구현, robotapp 수동 검증 완료 |
| 2C | ✅ 완료 | Async Commands — polling, single-flight, ACCEPTED [104] |
| 3A ~ 5 | 🔲 미착수 | 세션, 로그, watch, ghost, scene diff, agent layer |

---

## Phase 2B 실제 반영 내용

- Plugin `PipeNameHelper.cs` 추가
- Plugin `MessageFraming.cs` 추가
- Core `MessageFraming.cs` 추가
- Plugin `IpcServer.cs` 구현
- Core `IpcTransport.cs` 구현
- `UnityctlBootstrap`에서 IPC 서버 시작
- `CommandExecutor` probe-first IPC 활성화
- `CreateIpcClientStream()` 3개 플랫폼 구현
- IPC 테스트 추가
- Linux 기본 빌드 출력명 `Game.x86_64` 보정

Phase 2B 구현 범위는 현재 코드 기준으로 아래처럼 보는 것이 정확합니다.

- 완료:
  - `ping`
  - `status`
  - `check`
  - `test`의 "started asynchronously" 의미 반환
  - 열린 Editor `build` 요청이 실제 BuildHandler까지 도달함
  - batch fallback
- 아직 후속 보강 필요:
  - domain reload 후 자동 IPC 복구를 더 강하게 재현/검증

---

## Phase 2C 실제 반영 내용

- `StatusCode.Accepted = 104` — Shared + Plugin 동기화
- `WellKnownCommands.TestResult = "test-result"` — Shared + Plugin 동기화
- Plugin `AsyncOperationRegistry.cs` — single-flight guard + age-check (360s) + TTL prune
- Plugin `TestHandler.cs` — `Accepted` 반환, `TestResultCollector` (ICallbacks + IErrorCallbacks, leaf-only)
- Plugin `TestResultHandler.cs` — `test-result` 멱등 조회 핸들러
- Plugin `UnityctlBatchEntry.cs` — Accepted 감지 → EditorApplication.update 폴링
- Plugin `UnityctlBootstrap.cs` — 60초 주기 Prune 훅
- CLI `AsyncCommandRunner.cs` — delegate 주입 폴링 (500ms 초기 → 1s 간격)
- CLI `TestCommand.cs` — `--no-wait`, `--timeout`, PlayMode 경고
- CLI `ConsoleOutput.cs` — `ACCEPTED [104]` Cyan 출력 분기
- CLI `Program.cs` — `test` 등록에 `noWait`, `timeout` 파라미터 반영
- `CommandCatalog` — Test에 `wait`, `timeout` 파라미터 추가
- 신규 테스트 `AsyncCommandRunnerTests.cs` (7개)
- CLI csproj `ConsoleAppFramework PrivateAssets=all` (test 참조 시 source generator 충돌 방지)

---

## 검증 현황

### 자동화 검증

기준: 2026-03-18 재실행

```bash
dotnet build unityctl.slnx
dotnet test unityctl.slnx
```

| 프로젝트 | 테스트 수 | 상태 |
|----------|----------|------|
| Unityctl.Shared.Tests | 19 | ✅ |
| Unityctl.Core.Tests | 30 | ✅ |
| Unityctl.Cli.Tests | 30 | ✅ |
| Unityctl.Integration.Tests | 6 | ✅ |

**총 85개 테스트 통과**

### robotapp 수동 검증

기준 프로젝트: `C:\Users\gmdqn\robotapp`

1. `unityctl init --project C:\Users\gmdqn\robotapp`
   - `Packages/manifest.json`에 `com.unityctl.bridge` 추가 확인
2. Unity Editor 열린 상태에서 `status`
   - 성공, `Ready`
3. Unity Editor 열린 상태에서 `ping`
   - 성공, `pong`
4. Unity Editor 열린 상태에서 `check`
   - 성공, `Compilation check passed`
5. Unity Editor 열린 상태에서 `test --mode edit`
   - 의도대로 `Busy`, `"Tests started asynchronously..."`
6. Unity Editor 열린 상태에서 `build`
   - IPC timeout이 아니라 실제 build 응답 확인
   - 현재 `robotapp` 컴파일 에러로 실패
7. Unity 재시작 후 `ping/status`
   - IPC 복구 확인
8. Unity 미실행 상태에서도 batch fallback 경로 동작 확인

현재 이 환경에서 안전하게 말할 수 있는 것은:

- IPC 경로로 `status/check/test-start`가 실제 응답함
- IPC 경로로 `ping/status/check/test-start`가 실제 응답함
- 열린 Editor `build`가 실제 BuildHandler 응답까지 도달함
- `dotnet build` / `dotnet test`가 통과함
- `init`가 외부 Unity 프로젝트에 올바른 plugin source를 기록함
- warmed state에서 `status` IPC 왕복은 대체로 `148~174ms` 수준
- warmed state에서 `ping` IPC 왕복은 대체로 `147~192ms` 수준

아직 추가 검증이 필요한 것은:

- domain reload 후 자동 IPC 복구를 더 확실하게 재현하고 닫는 일
- batch worker가 IPC 서버를 절대 띄우지 않는지에 대한 명시적 로그 검증
- pure transport-only latency를 프로세스 시작 오버헤드와 완전히 분리한 측정

---

## 실제 동작 상태

| 명령어 | 상태 | 비고 |
|--------|------|------|
| `unityctl` | ✅ | 버전 + 도움말 출력 |
| `unityctl --help` | ✅ | 전체 커맨드 표시 |
| `unityctl editor list` | ✅ | 설치된 Editor 탐지 |
| `unityctl init` | ✅ | `src/Unityctl.Plugin` 절대 경로 기록 |
| `unityctl ping` | ✅ | batch + IPC 모두 사용 가능 |
| `unityctl status` | ✅ | batch + IPC 모두 검증 |
| `unityctl check` | ✅ | `isCompiling` / `scriptCompilationFailed` 반영 |
| `unityctl test` | ✅ | IPC 폴링으로 실제 결과 수집, `--no-wait`/`--timeout` 지원 |
| `unityctl build` | ⚠️ | 열린 Editor transport는 확인됨, 현재 `robotapp` 컴파일 에러로 build 실패 |
| `unityctl tools` | ✅ | `CommandCatalog` 기반 메타데이터 출력 |
| `unityctl tools --json` | ✅ | 단일 catalog 기반 JSON discovery 출력 |

---

## 현재 코드 상태에서 주의할 점

- Plugin 코드는 `dotnet build`로 컴파일되지 않으므로 Unity Editor 확인이 필요합니다.
- `Shared`와 `Plugin/Shared`는 물리적으로 통합하지 않았습니다. 프로토콜 변경 시 양쪽 동기화가 필요합니다.
- `TestHandler`는 Phase 2C에서 `Accepted` + polling 모델로 실제 결과 수집을 완료했습니다.
- PlayMode 테스트는 domain reload로 콜백 소실 위험 → `--wait` 강제 비활성화됩니다. PlayMode completion handoff는 Phase 3A 범위입니다.
- `CheckHandler`는 실제 compiler errors 존재 여부는 반영하지만, 개별 에러 목록을 구조화해서 반환하지는 않습니다.

---

## 다음 단계

1. Phase 2B 후속 보강
   - domain reload 후 자동 IPC 복구 검증 강화
   - batch worker IPC 미기동 로그 검증
   - pure transport-only latency 측정
2. Phase 3A Session Layer 착수
3. Phase 1C 잔여
   - `release.yml`
   - README 정비
4. 문서 드리프트 방지
   - `docs/status/PROJECT-STATUS.md`
   - `docs/ref/phase-roadmap.md`
   - `CLAUDE.md`

---

## 기술 결정 로그

| 날짜 | 결정 | 이유 |
|------|------|------|
| 2026-03-17 | .NET 10 유지 | 현재 개발 머신과 테스트 환경이 .NET 10 기준으로 안정화됨 |
| 2026-03-17 | `CommandCatalog` 도입 | CLI 등록 정보와 `tools --json` drift 제거 |
| 2026-03-17 | `CommandRunner` 도입 | CLI command boilerplate 공통화 |
| 2026-03-17 | `PluginSourceLocator` 도입 | `init`의 하드코딩 경로 제거 |
| 2026-03-17 | `UnityVersionComparer` 도입 | 문자열 정렬 대신 버전 파싱 기반 선택 |
| 2026-03-17 | `test`는 2B에서 비동기 시작 의미만 반환 | 즉시 성공으로 오해되는 동작 제거, 실제 완료는 2C로 분리 |
| 2026-03-17 | IPC는 probe-first | probe 실패 시에만 batch 폴백해 중복 실행 방지 |
| 2026-03-18 | Polling 모델 채택 (Phase 2C) | IPC listen loop 직렬 구조상 장기 점유 차단 방지, 핸들러 메인 스레드 데드락 방지 |
| 2026-03-18 | Single-flight guard + age-check | 콜백 혼선 방지 (360s maxAge, Running TTL 10분) |
| 2026-03-18 | PlayMode --wait 강제 비활성화 | domain reload로 static registry 초기화 + 콜백 소실 |
| 2026-03-18 | ConsoleAppFramework PrivateAssets=all | 테스트 프로젝트 참조 시 source generator 타입 충돌 방지 |
