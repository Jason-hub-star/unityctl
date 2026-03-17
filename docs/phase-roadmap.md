# unityctl 전체 Phase 로드맵 (업데이트)

> 최종 업데이트: 2026-03-17
> 목표: MCP를 완전 대체할 수 있는 Unity AI 도구 체인

---

## MCP 대체 매핑

MCP 2025-11-25 스펙 기준으로 unityctl이 대체해야 할 핵심 기능:

| MCP 기능 | 설명 | unityctl 대응 | Phase | 실현 가능성 |
|----------|------|-------------|-------|:----------:|
| **Tools** | 도구 실행 | CLI 커맨드 + `--json` | ✅ 완료 | 100% |
| **tools/list** | 도구 목록 동적 발견 | `unityctl tools --json` | ✅ 완료 | 100% |
| **Resources** | 읽기 전용 데이터 제공 | scene snapshot, flight log | 3B, 4B | 82-95% |
| **Prompts** | 재사용 템플릿 | `ai-quickstart.md` | ✅ 완료 | 100% |
| **Tasks** | 장시간 작업 추적 | Session Layer | 3A | 95% |
| **Streaming** | 실시간 알림 | Watch Mode (IPC 스트림) | 3C | 88% |
| **Server Discovery** | 서버 발견 | `unityctl tools --json` + 메타데이터 | ✅ 완료 | 100% |

### MCP 대체 vs 상위 호환

unityctl은 MCP를 **대체**하는 것이 아니라 **상위 호환**을 목표로 합니다:
- CLI 기반이므로 모든 AI 에이전트 (Claude Code, Cursor, Copilot 등)가 즉시 사용 가능
- MCP 브릿지가 필요한 경우: MCP C# SDK v1.0의 `[McpToolType]` 어트리뷰트로 ~100줄 래핑 가능
- 참고: [MCP C# SDK v1.0](https://devblogs.microsoft.com/dotnet/release-v10-of-the-official-mcp-csharp-sdk/)

---

## Phase 구조

```
Phase 0   — 프로젝트 골격         ✅ 완료
Phase 0.5 — Plugin 부트스트랩     ✅ 완료
Phase 1A  — CLI 기본              ✅ 완료
Phase 1B  — 핵심 기능             ✅ 완료
Phase 1C  — 테스트 + 배포         ⚠️ 부분 완료
Phase 2A  — Foundation            ✅ 완료
Phase 2A+ — Tools Metadata        ✅ 완료  ← NEW
Phase 2B  — IPC Transport         🔲 미착수
Phase 2C  — Async Commands        🔲 미착수
Phase 3A  — Session Layer         🔲 미착수
Phase 3B  — Flight Recorder       🔲 미착수
Phase 3C  — Watch Mode            🔲 미착수
Phase 4A  — Ghost Mode            🔲 미착수
Phase 4B  — Scene Diff            🔲 미착수
Phase 5   — Agent Layer           🔲 미착수
```

---

## Phase별 상세 설계 + 실현 가능성 분석

### Phase 2B — IPC Transport (92%)

> 상세 설계: [phase-2b-plan.md](./phase-2b-plan.md)

**핵심 구현:**
- Plugin: `IpcServer.cs` — NamedPipeServerStream + 동기 메서드 + 백그라운드 Thread
- Core: `IpcTransport.cs` — NamedPipeClientStream 클라이언트
- 메시지 프레이밍: 4바이트 길이 헤더 (LE) + UTF-8 JSON
- `CommandExecutor`: IPC → Batch 자동 폴백

**리스크 및 대응:**
| 리스크 | 확률 | 대응 |
|--------|:----:|------|
| Unity 비동기 파이프 런타임 에러 | 높음 | 동기 메서드 + Thread (웹 리서치로 검증됨) |
| 도메인 리로드 시 파이프 서버 중단 | 중간 | `AssemblyReloadEvents` Stop/Start |
| 크로스 플랫폼 파이프명 | 낮음 | .NET이 자동 처리 (Win=NamedPipe, Linux/Mac=UDS) |

**웹 리서치 근거:**
- Unity 포럼에서 NamedPipeServerStream 동기 패턴 검증됨
- .NET의 NamedPipeServerStream이 Linux/macOS에서 자동으로 UDS 사용

---

### Phase 2C — Async Commands (87%)

**핵심 구현:**
- `IAsyncUnityctlCommand` 인터페이스 (TimeoutSeconds 프로퍼티)
- `TestHandler` → 비동기 구현 (TestRunnerApi 콜백 기반)
- `UnityctlBatchEntry`에서 비동기 대기 + 타임아웃

**리스크 및 대응:**
| 리스크 | 확률 | 대응 |
|--------|:----:|------|
| Unity TestRunnerApi 콜백 타이밍 | 중간 | `TaskCompletionSource` + 명시적 타임아웃 |
| batchmode에서 비동기 대기 중 Unity 종료 | 낮음 | `EditorApplication.quitting`에서 강제 완료 |

---

### Phase 3A — Session Layer (95%)

> MCP의 **Tasks** 기능 대응

**핵심 구현:**
- `SessionManager`: 세션 생성/조회/종료
- `SessionState`: Pending → Active → Completed/Failed
- `SessionStore`: `~/.unityctl/sessions/` 디렉토리에 JSON 파일

**CLI:**
```
unityctl session start --project /path
unityctl session list
unityctl session stop <id>
```

**리스크:** 극히 낮음. 파일 I/O + JSON 직렬화만 필요.

---

### Phase 3B — Flight Recorder (95%)

> MCP의 **Resources** 기능 일부 대응 (로그 데이터 제공)

**핵심 구현:**
- `FlightLog`: NDJSON 형식, 보존 정책 (크기/시간 기반)
- `FlightEntry`: 타임스탬프, 커맨드, 요청/응답, 소요 시간
- 모든 `CommandExecutor.ExecuteAsync()` 호출 시 자동 기록

**CLI:**
```
unityctl log --project /path
unityctl log --project /path --tail 20
unityctl log --project /path --since 1h
```

**리스크:** 극히 낮음. NDJSON append + 파일 회전만 필요.

---

### Phase 3C — Watch Mode (88%) ← 개선됨 (기존 75%)

> MCP의 **Streaming** 기능 대응

**핵심 구현:**
- Plugin Watchers:
  - `ConsoleWatcher`: `Application.logMessageReceivedThreaded` (스레드 세이프)
  - `HierarchyWatcher`: `EditorApplication.hierarchyChanged`
  - `CompileWatcher`: `CompilationPipeline.compilationStarted/Finished`
- IPC 스트리밍: `EventEnvelope` + `ConcurrentQueue` → IPC 파이프로 전송

**CLI:**
```
unityctl watch console --project /path --follow
unityctl watch hierarchy --project /path
unityctl watch compile --project /path
```

**개선 근거 (75% → 88%):**
Unity 콜백 API가 전부 웹 리서치로 검증됨:
- [`Application.logMessageReceivedThreaded`](https://docs.unity3d.com/ScriptReference/Application-logMessageReceivedThreaded.html) — 메인 스레드 안 탔어도 동작
- [`EditorApplication.hierarchyChanged`](https://docs.unity3d.com/ScriptReference/EditorApplication-hierarchyChanged.html) — GO 생성/삭제/이름변경 감지
- [`CompilationPipeline`](https://docs.unity3d.com/ScriptReference/Compilation.CompilationPipeline.html) — 컴파일 시작/완료 이벤트

**구현 전략 변경:**
```
기존: 커스텀 스트리밍 프로토콜 필요
개선: IPC(Phase 2B)의 EventEnvelope + ConcurrentQueue 활용
     Unity 콜백 → ConcurrentQueue<EventEnvelope> → IPC 파이프 스트림
```

**리스크 및 대응:**
| 리스크 | 확률 | 대응 |
|--------|:----:|------|
| Backpressure (이벤트 과다) | 중간 | 큐 크기 제한 + 오래된 이벤트 드롭 |
| 도메인 리로드 중 이벤트 유실 | 중간 | `AssemblyReloadEvents`에서 재등록 |
| IPC 연결 끊김 시 이벤트 누적 | 낮음 | 큐 크기 초과 시 자동 폐기 |

---

### Phase 4A — Ghost Mode (92%)

**핵심 구현:**
- `--dry-run` 플래그를 build, test, check 커맨드에 추가
- `BuildGhost`: BuildPipeline 호출 없이 설정 검증만 수행
- `TestGhost`: 테스트 목록 수집만 (실행 없음)

**CLI:**
```
unityctl build --project /path --dry-run --json
unityctl test --project /path --dry-run --json
```

**리스크:** 낮음. 기존 핸들러에 분기 로직 추가만 필요.

---

### Phase 4B — Scene Diff (82%) ← 대폭 개선 (기존 60%)

> MCP의 **Resources** 기능 대응 (씬 데이터 제공)

**개선 근거 (60% → 82%):**

기존 설계: 외부에서 Unity YAML 파싱 → 커스텀 diff 엔진 (복잡)

**새 설계: Unity SerializedObject API 활용 (Plugin 내부에서 실행)**

핵심 발견:
- [`SerializedProperty.DataEquals()`](https://docs.unity3d.com/ScriptReference/SerializedProperty.DataEquals.html) — Unity 내장 값 비교 API
- `SerializedObject.GetIterator()` + `Next()` → 모든 프로퍼티 순회 가능
- 프리팹 오버라이드도 Unity가 알아서 처리 (YAML 파싱 불필요!)

**구현 전략:**
```
1. SceneSnapshot (Plugin Handler):
   - 현재 씬의 모든 GameObject 트리 순회
   - 각 Component의 SerializedObject → 프로퍼티 맵 (JSON 직렬화)
   - IPC/Batch로 반환

2. SceneDiff (Plugin Handler):
   - 두 스냅샷의 프로퍼티별 비교 (DataEquals 사용)
   - 변경/추가/삭제된 프로퍼티 목록 반환

3. CLI:
   unityctl scene snapshot --project /path --json
   unityctl scene diff --project /path --before <id> --after <id> --json
```

**대안 (외부 YAML 파싱이 필요한 경우):**
- [VYaml](https://github.com/hadashiA/VYaml) — YamlDotNet 대비 6배 빠름, Unity `stripped` 태그 네이티브 지원

**리스크 및 대응:**
| 리스크 | 확률 | 대응 |
|--------|:----:|------|
| 대규모 씬 성능 (10,000+ GO) | 중간 | 필터링 옵션 (--path, --component) |
| 중첩 프리팹 구조 복잡도 | 중간 | Unity API가 플래튼 처리 |
| 스냅샷 저장 공간 | 낮음 | JSON 압축 + 보존 정책 |

---

### Phase 5 — Agent Layer (85%) ← 개선됨 (기존 70%)

**개선 근거 (70% → 85%):**

기존 설계: 커스텀 DSL 파서 필요 (`unityctl exec "check → test → build"`)

**새 설계: JSON 워크플로우 정의 (DSL 파싱 불필요)**

```
unityctl exec --workflow workflow.json

workflow.json:
{
  "steps": [
    { "command": "check", "params": { "project": "/path", "type": "compile" }, "onFail": "stop" },
    { "command": "test", "params": { "project": "/path", "mode": "edit" }, "onFail": "stop" },
    { "command": "build", "params": { "project": "/path", "target": "StandaloneWindows64" } }
  ]
}
```

**핵심 원칙:** unityctl은 프리미티브 (개별 도구)를 제공하고, 오케스트레이션은 외부 소비자 (AI 에이전트)에게 위임.

`unityctl exec`은 **편의 기능**이지 핵심이 아님. AI 에이전트는 이미 개별 커맨드를 순차적으로 호출할 수 있음.

**웹 리서치 근거:**
- [Workflow Core](https://github.com/danielgerlag/workflow-core) — .NET Standard 워크플로우 엔진, JSON/YAML DSL 내장
- 하지만 외부 라이브러리 의존성 추가 대신 자체 단순 구현 권장 (~200줄)

**MCP 호환 래퍼 (선택적, Phase 5 이후):**
```csharp
// MCP C# SDK v1.0을 활용하여 unityctl 커맨드를 MCP 서버로 래핑
// ~100줄 브릿지로 MCP 호환 서버 생성 가능
[McpToolType]
public class UnityctlMcpBridge
{
    [McpTool("unity_build")]
    public async Task<string> Build(string project, string target) { ... }
}
```

**리스크 및 대응:**
| 리스크 | 확률 | 대응 |
|--------|:----:|------|
| 조건 분기/에러 핸들링 설계 | 중간 | 단순 onFail: stop/continue/retry |
| JSON 워크플로우 스키마 검증 | 낮음 | JSON Schema validation |

---

## 전체 가능성 요약

| Phase | 내용 | 가능성 | MCP 대응 |
|-------|------|:------:|----------|
| 2A+ | Tools Metadata | **100%** | ✅ tools/list |
| 2B | IPC Transport | **92%** | — |
| 2C | Async Commands | **87%** | — |
| 3A | Session Layer | **95%** | Tasks |
| 3B | Flight Recorder | **95%** | Resources (일부) |
| 3C | Watch Mode | **88%** | Streaming |
| 4A | Ghost Mode | **92%** | — |
| 4B | Scene Diff | **82%** | Resources (씬 데이터) |
| 5 | Agent Layer | **85%** | 오케스트레이션 |

```
MCP 대체 가능 수준 (Phase 3B까지):  92%
전체 로드맵 완성 가능성:              89%
```

---

## 기술 결정 로그 (추가)

| 날짜 | 결정 | 이유 |
|------|------|------|
| 2026-03-17 | `unityctl tools --json` 추가 | MCP `tools/list` 대응, AI 에이전트의 동적 도구 발견 |
| 2026-03-17 | ToolsJsonContext 별도 source-gen | Shared에 CLI 타입 의존성 방지 (의존성 방향 유지) |
| 2026-03-17 | Scene Diff에 SerializedObject API 사용 | YAML 파싱 복잡도 제거, Unity 네이티브 API가 프리팹 처리 |
| 2026-03-17 | Phase 5에서 커스텀 DSL 대신 JSON 워크플로우 | ANTLR 등 외부 파서 의존성 불필요, 구현 단순화 |
| 2026-03-17 | MCP 호환 래퍼를 선택적 확장으로 분류 | MCP C# SDK v1.0 존재하므로 필요시 ~100줄로 브릿지 가능 |

---

## 참조 소스

### MCP 스펙 & SDK
- [MCP 2025-11-25 Specification](https://modelcontextprotocol.io/specification/2025-11-25)
- [MCP C# SDK v1.0](https://devblogs.microsoft.com/dotnet/release-v10-of-the-official-mcp-csharp-sdk/)
- [MCP C# SDK GitHub](https://github.com/modelcontextprotocol/csharp-sdk)

### Unity API (웹 리서치로 검증)
- [Application.logMessageReceivedThreaded](https://docs.unity3d.com/ScriptReference/Application-logMessageReceivedThreaded.html)
- [EditorApplication.hierarchyChanged](https://docs.unity3d.com/ScriptReference/EditorApplication-hierarchyChanged.html)
- [CompilationPipeline](https://docs.unity3d.com/ScriptReference/Compilation.CompilationPipeline.html)
- [SerializedProperty.DataEquals](https://docs.unity3d.com/ScriptReference/SerializedProperty.DataEquals.html)

### 워크플로우 & DSL
- [Workflow Core (JSON/YAML DSL)](https://github.com/danielgerlag/workflow-core)
- [VYaml (고성능 Unity YAML 파서)](https://github.com/hadashiA/VYaml)
- [ANTLR C# DSL 파서](https://dev.to/santoshmnrec/creating-external-dsls-using-antlr-and-c-51fj)
