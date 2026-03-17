# unityctl 개발 진행 상황

> 최종 업데이트: 2026-03-17

## 프로젝트 개요

| 항목 | 값 |
|------|-----|
| 레포 위치 | `C:\Users\ezen601\Desktop\Jason\unityctl` |
| 런타임 | .NET 10 (SDK 10.0.104, 계획은 .NET 8) |
| CLI 프레임워크 | ConsoleAppFramework 5.3.3 (Cysharp) |
| Shared 타겟 | .NET Standard 2.1 |
| Core 타겟 | .NET 10 |
| Unity 플러그인 | UPM, Unity 2021.3+, Newtonsoft.Json 3.2.1 |
| 테스트 프레임워크 | xUnit 2.9.2 |
| 테스트 프로젝트 | `C:\Users\ezen601\Desktop\Jason\robotapp2` (Unity 6000.0.64f1) |

---

## 아키텍처

```
unityctl.slnx
├── src/Unityctl.Shared    (netstandard2.1)  프로토콜 + 모델 + transport 인터페이스
├── src/Unityctl.Core      (net10.0)         핵심 비즈니스 로직 (transport, discovery, retry)
├── src/Unityctl.Cli       (net10.0)         얇은 CLI 셸 (Core에 위임)
├── src/Unityctl.Plugin    (Unity UPM)       Editor 브릿지 (Newtonsoft.Json 기반)
├── tests/Unityctl.Shared.Tests              프로토콜 단위 테스트
├── tests/Unityctl.Core.Tests                Core 로직 단위 테스트
├── tests/Unityctl.Cli.Tests                 Core 참조 단위 테스트
└── tests/Unityctl.Integration.Tests         Black-box CLI 테스트
```

### 의존성 방향

```
Shared ← Core ← Cli
  ↑        ↑       ↑
  │        │       └── Integration.Tests
  │        └────────── Core.Tests, Cli.Tests
  └─────────────────── Shared.Tests
```

Plugin은 솔루션 외부 (Unity 내에서만 컴파일). Shared~/ 폴더에 프로토콜 타입 복사본 유지.

---

## Phase 진행 체크리스트

### Phase 0 — 프로젝트 골격 ✅ 완료
> 커밋: `5fba596`

- [x] 새 레포 생성, `unityctl.sln`, Directory.Build.props
- [x] `Unityctl.Shared` csproj (netstandard2.1)
- [x] `Unityctl.Cli` csproj (net10.0, ConsoleAppFramework)
- [x] StatusCode enum (0/1xx/2xx/5xx 분류)
- [x] CommandRequest / CommandResponse 프로토콜
- [x] WellKnownCommands (ping, status, build, test, check)
- [x] JsonContext (System.Text.Json Source Generator)
- [x] Models: UnityEditorInfo, BuildRequest/Result, TestRequest/Result, CheckResult
- [x] Constants (버전, 파이프 접두사, 타임아웃)
- [x] IPlatformServices 인터페이스
- [x] WindowsPlatform / MacOsPlatform / LinuxPlatform 스켈레톤
- [x] PlatformFactory (런타임 OS 감지)
- [x] `dotnet build` 통과
- [x] Shared 테스트 프로젝트 기본 통과

### Phase 0.5 — Plugin 부트스트랩 ✅ 완료
> 커밋: `98e1f4e`

- [x] `Unityctl.Plugin` UPM 구조 (package.json, asmdef)
- [x] Shared 타입 소스 복사 (StatusCode, CommandRequest, CommandResponse)
- [x] `UnityctlBatchEntry.cs`: `--unityctl-command/request/response` 파싱
- [x] `IUnityctlCommand` 인터페이스
- [x] `PingHandler`: StatusCode.Ready + 버전 정보 반환
- [x] `StatusHandler`: Editor 상태 → StatusCode 변환
- [x] `CommandRegistry`: TypeCache 기반 자동 탐색
- [x] `IpcRequestRouter`: command → handler 디스패치
- [x] `UnityctlBootstrap`: InitializeOnLoad 자동 초기화
- [x] CLI 커맨드 스캐폴드 (init, editor list, status, build, test, check)
- [x] `unityctl init` 명령어 (manifest.json 패키지 추가)
- [x] ConsoleAppFramework delegate 기반 등록 방식 적용

### Phase 1-A — CLI 기본 ✅ 완료
> 커밋: `733f702`

- [x] `UnityEditorDiscovery`: editors.json 파싱 (Win/Mac)
- [x] `UnityEditorDiscovery`: 파일시스템 스캔 폴백
- [x] `UnityEditorDiscovery`: ProjectVersion.txt로 프로젝트별 에디터 매칭
- [x] `BatchModeRunner`: Unity 프로세스 스폰 + request/response 파일 관리
- [x] `BatchModeRunner`: 프로세스 타임아웃 + 킬 처리
- [x] `BatchModeRunner`: 프로젝트 잠금 감지 (UnityLockfile)
- [x] `BatchModeRunner`: 실패 시 로그 tail 60줄 포함
- [x] `RetryPolicy`: StatusCode 기반 분류 (Transient/Fatal/Error)
- [x] `RetryPolicy`: 지수 백오프 (BaseDelay × 2^attempt, MaxDelay 캡)
- [x] `ConsoleOutput`: 컬러 출력 + 복구 힌트 메시지
- [x] `JsonOutput`: `--json` 플래그 지원
- [x] 모든 커맨드 인프라 연결 완료
- [x] `unityctl editor list` 실제 동작 확인 (6000.0.64f1, 2022.3.62f3 발견)

### Phase 1-B — 핵심 기능 ✅ 완료
> 커밋: `0fd744b`

- [x] `BuildHandler`: BuildPipeline.BuildPlayer 연동
- [x] `BuildHandler`: 타겟 파싱 (StandaloneWindows64, OSX, Linux, Android, iOS, WebGL)
- [x] `BuildHandler`: 씬 검증, 기본 출력 경로, 빌드 리포트 수집
- [x] `TestHandler`: TestRunnerApi 연동 (EditMode/PlayMode)
- [x] `TestHandler`: TestResultCollector 콜백
- [x] `CheckHandler`: CompilationPipeline 어셈블리 체크
- [x] 모든 CLI 커맨드에 `--json` 플래그 전달
- [ ] **실제 batchmode 통합 테스트** (robotapp2에서 ping round-trip)
- [ ] **실제 batchmode 통합 테스트** (robotapp2에서 compile check)
- [ ] **실제 batchmode 통합 테스트** (robotapp2에서 EditMode test)
- [ ] **실제 batchmode 통합 테스트** (robotapp2에서 build)

### Phase 1-C — 테스트 + 배포 ⚠️ 부분 완료
> 커밋: `7563d55`

- [x] GitHub Actions `ci-dotnet.yml` (Win/Mac/Linux 매트릭스)
- [x] GitHub Actions `ci-unity.yml` (GameCI, 수동 트리거)
- [x] `docs/getting-started.md`
- [x] `docs/ai-quickstart.md`
- [ ] GitHub Actions `release.yml` (framework-dependent publish, 4 RID)
- [ ] 통합 테스트 시나리오 자동화
- [ ] README.md (30초 데모, 3-step install, 실패 복구 안내)

### Phase 2A — Foundation ✅ 완료

> 직렬화 수정 + Payload 타입 고정 + Core 라이브러리 추출 + 테스트 2-tier 구축

**2A-1. Payload 타입 전환** (결정 1):
- [x] `CommandRequest.Parameters`: `Dictionary<string, object?>` → `JsonObject?`
- [x] `CommandResponse.Data`: `Dictionary<string, object?>` → `JsonObject?`
- [x] `CommandRequest.GetParam()` 문자열 편의 접근자
- [x] `CommandRequest.GetParam<T>()` typed accessor (int, bool, long, double)
- [x] `CommandRequest.GetObjectParam()` 중첩 JsonObject 접근자
- [x] `JsonContext`에 `JsonObject`, `JsonNode`, `EventEnvelope`, `SessionInfo` 등록
- [x] Plugin Shared~: `Dictionary<string, object>` → `JObject` (Newtonsoft)
- [x] Plugin `CommandRequest.GetParam()` + `GetParam<T>()` + `GetObjectParam()` 동일 API
- [x] 모든 Handler: `request.GetParam()` 패턴으로 통일 (private `GetParam` 헬퍼 제거)

**2A-2. Plugin 직렬화 전환**:
- [x] `package.json`에 `com.unity.nuget.newtonsoft-json: 3.2.1` 의존성 추가
- [x] `UnityctlBatchEntry.cs`: `JsonUtility.FromJson/ToJson` → `JsonConvert.DeserializeObject/SerializeObject`
- [x] 모든 Handler 반환값: `Dictionary<string, object>` → `JObject`

**2A-3. 파이프명 정규화** (결정 2):
- [x] `Constants.NormalizeProjectPath()` — Windows 대소문자, 슬래시 통일, 후행 슬래시 제거
- [x] `Constants.GetPipeName()` — SHA256 해시 기반 결정적 파이프명
- [x] `Constants.GetConfigDirectory()` — `~/.unityctl/` 경로

**2A-4. Core 라이브러리 추출**:
- [x] `Unityctl.Core.csproj` (net10.0 Library, Shared 참조)
- [x] Platform/ 전체 이동: IPlatformServices, Windows/Mac/Linux, PlatformFactory
- [x] Discovery/ 이동: UnityEditorDiscovery + UnityProcessDetector (stub)
- [x] Retry/ 이동: RetryPolicy
- [x] Transport/ 신규: `ITransport`, `TransportCapability`, `BatchTransport`, `IpcTransport` (stub), `CommandExecutor`
- [x] FlightRecorder/ 신규: `FlightLog`, `FlightEntry` (stub)
- [x] Protocol/ 신규: `EventEnvelope`, `SessionInfo`
- [x] CLI에서 Platform/, Infrastructure/ 디렉토리 삭제 (Core로 완전 이동)

**2A-5. CLI 간소화 + 테스트 2-tier** (결정 4):
- [x] `Cli.csproj` → Core + Shared 참조
- [x] 모든 CLI 커맨드가 Core `CommandExecutor`에 위임
- [x] `Cli.Tests.csproj` → Core 참조 (Cli 직접 참조 제거 → AppLocker 해결)
- [x] `Core.Tests/` 신규: PipeNameTests(6개) + RetryPolicyTests(12개)
- [x] `Integration.Tests/` 신규: Black-box CLI 테스트 4개
  - CLI 빌드 의존성 (`ProjectReference` with `ReferenceOutputAssembly=false`)
  - `ArgumentList.Add()` 개별 추가 (공백 경로 안전)
  - AppLocker 감지 + graceful skip (exit code `-532462766` 탐지)
  - stdout/stderr 동시 읽기 (deadlock 방지)
- [x] `unityctl.slnx`에 Core + Core.Tests + Integration.Tests 추가

---

## Phase 2A+ — Tools Metadata ✅ 완료

- [x] `ToolsCommand.cs` — 전체 도구 메타데이터 정의 (name, description, category, parameters)
- [x] `unityctl tools` — 사람 읽기용 도구 목록
- [x] `unityctl tools --json` — 기계 판독용 JSON 배열 (MCP `tools/list` 대응)
- [x] `ToolsJsonContext` source-generated serializer
- [x] Integration tests: `Tools_PrintsToolList`, `Tools_Json_ReturnsValidJsonArray`

## Phase 2B — IPC Transport + 프로세스 감지 🔲 미착수

- [ ] Plugin `IpcServer.cs` 구현 (Named Pipe / UDS)
- [ ] Core `IpcTransport.cs` 구현 (Named Pipe 클라이언트)
- [ ] `UnityProcessDetector` 구현 (WMI / ps)
- [ ] `CommandExecutor` IPC 우선 + Batch 폴백 로직
- [ ] `UnityctlBootstrap`에서 IpcServer 시작

## Phase 2C — Async Commands + Batch 수정 🔲 미착수

- [ ] `IAsyncUnityctlCommand` 인터페이스 (TimeoutSeconds)
- [ ] `TestHandler` → IAsyncUnityctlCommand 구현
- [ ] `UnityctlBatchEntry` 비동기 대기 로직 + 타임아웃
- [ ] `CheckHandler` 실제 컴파일 결과 수집
- [ ] compile-only 재시도 로직

## Phase 3A — Session Layer 🔲 미착수

- [ ] SessionManager, SessionState, SessionStore
- [ ] CLI: `session start`, `session list`, `session stop`

## Phase 3B — Flight Recorder 🔲 미착수

- [ ] FlightLog 전체 구현 (NDJSON, 보존, 검색)
- [ ] CLI: `unityctl log`, `unityctl log --tail`

## Phase 3C — Watch Mode 🔲 미착수

- [ ] ConsoleWatcher, HierarchyWatcher, SelectionWatcher
- [ ] CLI: `unityctl watch console`

## Phase 4A — Ghost Mode 🔲 미착수

- [ ] `--dry-run` 플래그
- [ ] BuildGhost, TestGhost

## Phase 4B — Scene Diff 🔲 미착수

- [ ] SceneSnapshot, SceneDiffEngine
- [ ] CLI: `unityctl scene snapshot`, `unityctl scene diff`

## Phase 5 — Agent Layer 🔲 미착수

- [ ] 외부 소비자 방식 (unityctl은 프리미티브만)
- [ ] `unityctl exec <expression>`

---

## 검증 현황

### 유닛 테스트

기준: `dotnet test unityctl.slnx` 실행 결과 (2026-03-17)

| 프로젝트 | 테스트 수 | 상태 | 비고 |
|----------|----------|------|------|
| Unityctl.Shared.Tests | 16 | ✅ 전체 통과 | 프로토콜 round-trip, typed accessor, EventEnvelope |
| Unityctl.Core.Tests | 18 | ✅ 전체 통과 | 파이프명 정규화(6), RetryPolicy(12) |
| Unityctl.Cli.Tests | 21 | ✅ 전체 통과 | PlatformFactory(2), EditorDiscovery(4), RetryPolicy(15) |
| Unityctl.Integration.Tests | 4 | ✅ 전체 통과 | NoArgs, Help, Status, EditorList |

**총 59개 테스트, 전체 통과.**

단, Integration.Tests는 환경 의존성이 있음:
- AppLocker가 CLI 실행을 차단하는 환경에서는 4개 테스트가 diagnostic 메시지 출력 후 skip (통과 처리)
- 현재 개발 환경에서는 4개 모두 실제 CLI 실행하여 통과
- CI 환경에서의 검증은 아직 미수행

### 실제 동작 검증

| 명령어 | 상태 | 비고 |
|--------|------|------|
| `unityctl` (no args) | ✅ | 버전 + 사용법 출력 |
| `unityctl --help` | ✅ | 전체 커맨드 목록 |
| `unityctl editor list` | ✅ | 6000.0.64f1 + 2022.3.62f3 발견 |
| `unityctl editor list --json` | ✅ | JSON 배열 출력 |
| `unityctl init` | ✅ | manifest.json 수정 |
| `unityctl status` | ⚠️ | 열린 프로젝트에서 `ProjectLocked` (IPC 미구현) |
| `unityctl build` | 🔲 | 실제 batchmode 통합 검증 전 |
| `unityctl test` | 🔲 | 실제 batchmode 통합 검증 전 |
| `unityctl check` | 🔲 | 실제 batchmode 통합 검증 전 |
| `unityctl tools` | ✅ | 7개 도구 목록 + 파라미터 출력 |
| `unityctl tools --json` | ✅ | MCP tools/list 대응 JSON 배열 |

### 통합 테스트 (robotapp2)

| 시나리오 | 상태 | 커맨드 |
|----------|------|--------|
| 열린 프로젝트 status | ⚠️ | `ProjectLocked` (IPC 미구현) |
| editor discovery | ✅ | 6000.0.64f1 목록 확인 |
| compile check | 🔲 | 미검증 |
| EditMode test | 🔲 | 미검증 |
| build | 🔲 | 미검증 |

---

## AI 작업 비용 / 출력량 비교

### 핵심 지표 요약

```
에디터 목록 조회: AI 토큰 98% 절감 (3,517 → 69)

  unity-cli list     : 14,070 chars (도구 스키마 전체)
  unityctl editor    :    278 chars (에디터 정보만)

  이유: unity-cli list는 등록된 도구 스키마 전체를 반환,
  unityctl은 에디터 정보만 반환. 같은 의도를 더 효율적으로 처리.
```

### 안전하게 주장 가능한 것

- ✅ **Compact structured output**: 에디터 탐색 출력량 98% 감소
- ✅ **Bootstrap automation**: `unityctl init`으로 플러그인 자동 설치
- ✅ **Path normalization**: 경로 구분자 이슈 없음
- ✅ **StatusCode 프로토콜**: 구조화된 에러 분류 + 자동 재시도
- ✅ **Typed payload**: JsonObject/JObject 기반, serializer 중립적 파라미터 전달
- ✅ **Tool discovery**: `unityctl tools --json`으로 AI 에이전트가 사용 가능한 커맨드를 동적 발견 (MCP `tools/list` 대체)

### 아직 주장하면 안 되는 것

- ❌ "AI 토큰 전체 작업에서 X% 절감" — status/build/test/check는 미검증
- ❌ "기존 방식보다 전체적으로 더 빠름" — IPC 없이 batchmode는 느림
- ❌ "open-editor workflow 우위" — IPC 미구현으로 열린 프로젝트 지원 불가

---

## 기술 결정 로그

| 날짜 | 결정 | 이유 |
|------|------|------|
| 2026-03-17 | .NET 10 사용 (계획은 .NET 8) | 개발 환경에 .NET 10만 설치됨 |
| 2026-03-17 | ConsoleAppFramework delegate 방식 | v5는 static class `Add<T>` 미지원 |
| 2026-03-17 | Shared 소스 직접 복사 | 타입 10개 미만, 자동화는 후순위 |
| 2026-03-17 | Plugin은 솔루션 빌드에 미포함 | Unity API 의존성 때문에 dotnet build 불가 |
| 2026-03-17 | batchmode 응답은 response-file 우선 | stdout/log 오염 방지, 구조화된 결과 유지 |
| 2026-03-17 | `Dictionary<string,object?>` → `JsonObject` | serializer마다 object 해석이 달라 파라미터 손실 발생 |
| 2026-03-17 | Plugin `JsonUtility` → `JsonConvert` (Newtonsoft) | JsonUtility는 Dictionary/동적 구조 미지원 |
| 2026-03-17 | Core 라이브러리 추출 | CLI 테스트에서 AppLocker 차단 해결 + transport 추상화 기반 |
| 2026-03-17 | Integration.Tests AppLocker 감지 | 정책 차단 시 skip (실패 대신 diagnostic 출력) |

---

## 다음 단계 (우선순위)

1. **Phase 3B — Flight Recorder** (2A와 독립, 빠른 성과)
2. **Phase 2B — IPC Transport** — 열린 Unity 프로젝트에서 실제 응답 반환
3. **Phase 2C — Async Commands** — TestHandler 실제 결과 반환
4. **batchmode 통합 검증** — robotapp2에서 check → test → build round-trip
5. **CI에서 Integration.Tests 실행 검증**
