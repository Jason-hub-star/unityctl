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
| Mcp.Tests | MCP 도구 등록, 블랙박스 (stdio 프로세스) | xUnit (McpClient) |
| Integration.Tests | CLI black-box | xUnit (프로세스 스폰) |

### 크로스 플랫폼 경로
테스트에서 파일 경로를 만들 때 `\`를 직접 쓰지 않는다. Linux/macOS CI에서 깨짐.
```csharp
// ❌ Linux에서 리터럴 백슬래시로 처리됨
ReadRepoFile(@"src\Unityctl.Plugin\Editor\Commands");
tempProject.Replace('/', '\\');

// ✅ 크로스 플랫폼
Path.Combine("src", "Unityctl.Plugin", "Editor", "Commands");
path.Replace('\\', Path.DirectorySeparatorChar);
```

- Integration.Tests는 AppLocker 감지 + graceful skip.
- Mcp.Tests의 `McpBlackBoxTests`는 빌드된 `unityctl-mcp` 바이너리를 프로세스로 띄운다. Debug를 Release보다 먼저 탐색한다 (`dotnet test`의 기본이 Debug이므로 stale Release 바이너리 방지).
- 테스트 필터: `dotnet test --filter "FullyQualifiedName!~Integration"`

### Flaky 테스트 정책

- PR 대상 Shared/Core/Cli/Mcp 테스트는 flaky 0개를 목표로 한다.
- "가끔 실패" 상태로 두지 않는다. 시간, 경로, 프로세스, 환경 의존성은 deterministic fixture나 주입 가능한 clock/delay/platform hook으로 고정한다.
- Unity Editor, AppLocker, 라이선스처럼 PR .NET gate에서 안정적으로 증명할 수 없는 항목은 Integration/Unity workflow로 격리하고 skip/preflight 이유를 명확히 남긴다.
- 새 버그 수정은 같은 PR에 재현 테스트를 추가한다. 특히 IPC timeout, AppLocker, batch fallback, dirty scene policy, parser edge case는 회귀 테스트 우선순위가 높다.
- `FlightLogRobustnessTests.Query_FilterByUntil_ExcludesNewerEntries`처럼 날짜/시각 경계가 원인인 테스트는 고정 시각 입력으로 안정화한다.
- 새 flaky가 발견되면 `.github/ISSUE_TEMPLATE/flaky-test.yml`로 CI run, OS, 반복 횟수, isolation/stabilization plan을 남긴다.
- 회귀 버그는 `.github/ISSUE_TEMPLATE/regression-bug.yml`로 신고하고, 수정 PR에는 실패 재현 테스트를 포함한다.
- 모든 PR은 `.github/PULL_REQUEST_TEMPLATE.md`의 Test Trust / Contract Safety / README User Path / Unity Reality Check 체크리스트를 따라 검증 범위를 명시한다.

### 새 명령 추가 체크리스트

새 명령은 한 레이어에만 추가되면 공개 API 신뢰를 깨뜨린다. 아래 경로를 같은 PR에서 모두 확인한다.

1. `WellKnownCommands`: Shared 상수를 추가하고 Plugin `Editor/Shared/WellKnownCommands.cs` 복사본을 동기화한다.
2. `CommandCatalog`: schema/tools에 노출될 정의, CLI 이름, 파라미터, 예시를 추가하고 `CommandCatalogTests`/`CommandSchemaTests` 기대값을 갱신한다.
3. CLI 등록: `src/Unityctl.Cli/Program.cs`에 verb를 등록하고 해당 CLI parser/request 테스트를 추가한다.
4. MCP allowlist/schema: read 명령은 `QueryTool`, write 명령은 `RunTool` allowlist에 넣고 MCP schema/black-box 테스트가 표면을 검증하게 한다.
5. Plugin handler 등록: `src/Unityctl.Plugin/Editor/Commands/*Handler.cs`에 handler를 추가하고 `CommandRegistry` 자동 등록/handler coverage guardrail을 통과시킨다.
6. 공개 문서: README, getting-started, quickstart, status 문서가 새 public surface와 검증 범위를 정확히 말하는지 확인한다.

최소 검증 세트:

```bash
dotnet test tests/Unityctl.Shared.Tests -c Release --filter "CommandCatalogTests|CommandSchemaTests|CommandSyncGuardrailTests"
dotnet test tests/Unityctl.Cli.Tests -c Release --filter "<새 명령 관련 테스트>"
dotnet test tests/Unityctl.Mcp.Tests -c Release
```

## §8. 파일 위치 규칙

| 유형 | 경로 |
|------|------|
| CLI 커맨드 | `src/Unityctl.Cli/Commands/{Name}Command.cs` |
| Plugin 핸들러 | `src/Unityctl.Plugin/Editor/Commands/{Name}Handler.cs` |
| 테스트 | `tests/Unityctl.{Layer}.Tests/{Name}Tests.cs` |
| 프로토콜 타입 | `src/Unityctl.Shared/Protocol/{Name}.cs` |
| Plugin 프로토콜 복사 | `src/Unityctl.Plugin/Editor/Shared/{Name}.cs` |

## §9. Plugin 디버깅

IPC 실패(statusCode 201) 시 디버깅 절차:

1. `unityctl doctor --project <path>` 실행 — IPC/Plugin/Editor 상태 한 방 확인
   연결/리로드 계열 실패 (`ProjectLocked`, `Busy`, `PluginNotInstalled`, `CommandNotFound`, IPC/pipe/reload/domain 포함 `UnknownError`)에서는 CLI가 doctor summary를 자동 출력한다.
2. Editor.log 직접 확인: `grep "error CS" "$LOCALAPPDATA/Unity/Editor/Editor.log" | tail -10`
3. **추측 수정 최대 1회**, 그래도 안 되면 Editor.log 에러 메시지 기반으로 수정

금지사항:
- Plugin `.cs` 파일에 `touch` 명령 사용 금지 (파일 내용이 비워질 수 있음)
- `.asmdef` 파일 수정/삭제 금지 (Plugin 전체 로드 불가)
- Bee 캐시(`Library/Bee/`) 삭제는 최후 수단으로만
