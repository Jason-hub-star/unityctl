# unityctl 코드 패턴 가이드

## §1. 빌드 설정

| 항목 | 값 |
|------|-----|
| LangVersion | 12 |
| Nullable | enable |
| TreatWarningsAsErrors | true |
| ImplicitUsings | enable |

## §2. 네이밍

| 대상 | 규칙 | 예시 |
|------|------|------|
| 클래스 | PascalCase, sealed 기본 | `sealed class CommandExecutor` |
| 인터페이스 | `I` 접두사 | `IPlatformServices`, `ITransport` |
| async 메서드 | `Async` 접미사 | `ExecuteAsync`, `ProbeAsync` |
| private 필드 | `_camelCase` | `_platform`, `_projectPath` |
| 상수 | PascalCase | `DefaultTimeoutMs`, `PipePrefix` |

## §3. 핵심 패턴

### Result 패턴
```csharp
CommandResponse.Ok(message, data)
CommandResponse.Fail(StatusCode.UnknownError, message, errors)
```
모든 커맨드는 예외 대신 `CommandResponse`를 반환.

### StatusCode 분류
- `0` = Ready (성공)
- `1xx` = Transient (재시도 가능: Compiling, Reloading, Busy, Accepted)
- `2xx` = Fatal (즉시 실패: NotFound, ProjectLocked)
- `5xx` = Error (명령 오류: CommandNotFound, BuildFailed)

### 생성자 주입
```csharp
new CommandExecutor(platform, discovery, retryPolicy)
```

### CancellationToken 전파
모든 async 메서드에 `CancellationToken ct` 매개변수 전달.

## §4. 직렬화

### CLI/Core (System.Text.Json)
```csharp
[JsonSerializable(typeof(CommandRequest))]
JsonSerializer.Serialize(request, UnityctlJsonContext.Default.CommandRequest)
```
- Source Generator 필수 — reflection 기반 사용 금지.
- 새 타입 추가 시 `JsonContext.cs`에 `[JsonSerializable]` 등록.

### Plugin (Newtonsoft.Json)
```csharp
JsonConvert.SerializeObject(response, Formatting.Indented)
JsonConvert.DeserializeObject<CommandRequest>(json)
```
- Unity 내 Newtonsoft 패키지 (`com.unity.nuget.newtonsoft-json: 3.2.1`).
- lowercase 필드명 + `[JsonProperty]` 어트리뷰트.

### Payload 타입
- CLI/Core: `JsonObject?` (System.Text.Json)
- Plugin: `JObject` (Newtonsoft)
- **`Dictionary<string, object?>` 사용 금지** — serializer 간 호환 깨짐.

## §5. Transport 계층

### IPC (Phase 2B)
- Wire: `[4-byte LE int: length][UTF-8 JSON body]`
- 서버: 동기 I/O + 백그라운드 Thread (Unity Mono 비동기 미검증)
- 클라이언트: 비동기 `NamedPipeClientStream`
- 전략: probe-first (실패 → batch 폴백, send 실패 → 에러 반환)

### Batch
- Unity batchmode 스폰 → request/response 파일
- 타임아웃: 10분 (`BatchModeTimeoutMs`)

## §6. Plugin 규칙

- `#if UNITY_EDITOR` 가드 필수 (비 Unity 환경 컴파일 방지)
- `.meta` 파일 직접 수정 금지 — Unity가 자동 생성
- `Shared/` 폴더는 Shared 프로젝트의 소스 복사본 — 원본 수정 시 동기화 필요
- batchmode 가드: `Application.isBatchMode` 체크로 IPC 서버 시작 방지

## §7. 테스트

| 계층 | 대상 | 방식 |
|------|------|------|
| Shared.Tests | 프로토콜 roundtrip, accessor | xUnit |
| Core.Tests | PipeName, RetryPolicy, IPC | xUnit |
| Cli.Tests | PlatformFactory, Discovery, AsyncCommandRunner | xUnit (Cli 참조) |
| Integration.Tests | CLI black-box | xUnit (프로세스 스폰) |

- Integration.Tests는 AppLocker 감지 + graceful skip.
- 테스트 필터: `dotnet test --filter "FullyQualifiedName!~Integration"`

## §8. 파일 위치 규칙

| 유형 | 경로 |
|------|------|
| CLI 커맨드 | `src/Unityctl.Cli/Commands/{Name}Command.cs` |
| Plugin 핸들러 | `src/Unityctl.Plugin/Editor/Commands/{Name}Handler.cs` |
| 테스트 | `tests/Unityctl.{Layer}.Tests/{Name}Tests.cs` |
| 프로토콜 타입 | `src/Unityctl.Shared/Protocol/{Name}.cs` |
| Plugin 프로토콜 복사 | `src/Unityctl.Plugin/Editor/Shared/{Name}.cs` |
