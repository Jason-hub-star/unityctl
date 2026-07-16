# GOAL-spatial-grounding — 씬을 "측정된 공간 사실"로 (스크린샷 없이 AI 공간 그라운딩)

## 골 한 줄
`spatial describe`/`spatial check` 신규 명령이 씬 지오메트리를 토큰-싸게 구조화 사실(월드 AABB·축·법선·술어 판정)로 반환 — verified by `dotnet test unityctl.slnx --filter "FullyQualifiedName!~Integration"` 전체 green + Shared/Cli/Mcp guardrail green + `dotnet build unityctl.slnx` 경고 0, while preserving 기존 890 테스트 green과 방금 고친 IPC reload 내성. details in docs/goals/GOAL-spatial-grounding.md

---

## 골 사다리 (직렬, 유닛당 골 1개 — 승인 게이트는 러닝 사이)

이 초기 브리프는 **러닝 A(공간 그라운딩)** 만 6요소 완전 명세한다. B/C/D는 아웃컴+검증 표면만 스케치하고, 해당 러닝에 도달할 때 이 문서를 확장한다.

| 러닝 | 유닛 | 왜 이 순서 | .NET 게이트 green 가능 범위 |
|---|---|---|---|
| **A (지금)** | 공간 그라운딩 `spatial describe`/`spatial check` | 자체완결 신규 명령. "천장 참사"를 직접 해결. B의 fleet 데모가 이 명령을 사용 | Shared/Cli/Mcp/tests 전부. Plugin 지오메트리 핸들러는 Unity-compile-pending |
| **B** | 에이전트 스웜(fleet) 모드 | 최우선 마케팅 차별점. A의 spatial 명령을 병렬 감사 데모에 활용 | Core 라우팅/동시성 테스트 + Cli. 실 병렬은 Unity live |
| **C** | 자율 검증-수정 루프 1급 명령 | B의 안전한 병렬 위에서 verdict 루프 | verdict 스키마 + orchestration 테스트 |
| **D** | CI/headless + GitHub Action | Unity MCP가 구조적으로 불가한 무에디터 파이프라인 | 워크플로 YAML 유효성 + WorkflowGuardrailTests |

**누적 제약**: 각 러닝의 Constraints에 "이전 러닝의 검증 표면 green 유지"를 누적한다.

---

## 러닝 A — 공간 그라운딩

### 1. Outcome (측정 가능한 완료 상태)
- 신규 read 명령 2종이 7계층(§7 체크리스트) 전부 동기화되어 존재:
  - `spatial describe <target>` → 월드 AABB(center/size/min/max) + 최단축/최장축 라벨 + 표면 법선 방향 + pivot vs bounds-center 편차. **summary-by-default(§10)**, `--full`로 상세.
  - `spatial check <a> <predicate> <b>` (predicate ∈ {covers, inside, on-top-of, overlaps, aligned}) → `pass: bool` + 수치 이유(footprint, 회전 오차 deg, gap m, 겹침 부피).
- 두 명령이 `QueryTool`(read) MCP 표면 + CLI verb + CommandCatalog schema에 노출.
- Plugin 핸들러 `SpatialDescribeHandler.cs`/`SpatialCheckHandler.cs`가 code-patterns 준수로 authored (Unity 컴파일 확인은 Unity Editor에서 — .NET 게이트 밖의 알려진 경계).

### 2. Verification surface (실행 에이전트가 직접 실행)
- 명령: `dotnet build unityctl.slnx` → 기대: 성공, 경고 0 (TreatWarningsAsErrors).
- 명령: `dotnet test tests/Unityctl.Shared.Tests -c Release --filter "CommandCatalogTests|CommandSchemaTests|CommandSyncGuardrailTests"` → 기대: green (spatial 2종이 catalog/schema에 존재, Plugin shared copy 동기화됨).
- 명령: `dotnet test tests/Unityctl.Cli.Tests -c Release --filter "Spatial"` → 기대: green (신규 CLI 파서/request 테스트).
- 명령: `dotnet test tests/Unityctl.Mcp.Tests -c Release` → 기대: green (QueryTool allowlist/schema 표면).
- 명령: `dotnet test unityctl.slnx --filter "FullyQualifiedName!~Integration"` → 기대: 전체 green (기존 890 + 신규 회귀 미발생).
- 아티팩트: `SpatialDescribeHandler.cs`, `SpatialCheckHandler.cs`, 신규 테스트 파일, 문서 갱신(README/getting-started/status).

### 3. Constraints (후퇴 금지)
- 기존 890 유닛 테스트 green 유지 (Shared 107 / Core 169 / Cli 589 / Mcp 25).
- `dotnet build` 경고 0 (TreatWarningsAsErrors=true).
- 방금 고친 IPC reload 내성(`IpcReloadStaleMs`) 및 asset-copy 경로 가드 미회귀.
- 신규 read 명령은 summary-by-default — `--full` 없이 큰 페이로드 흘리지 않음(§10).
- `Dictionary<string,object?>` 금지, Payload는 `JObject`(Plugin)/`JsonObject`(Core).

### 4. Boundaries
- 허용: `src/Unityctl.Shared/**`(WellKnownCommands, CommandCatalog), `src/Unityctl.Cli/**`(Program.cs 등록 + Commands), `src/Unityctl.Mcp/**`(QueryTool/schema), `src/Unityctl.Plugin/Editor/Commands/**`(신규 핸들러 + `Editor/Shared/WellKnownCommands.cs` 복사본), `tests/**`, `docs/**`.
- 금지: `src/Unityctl.Core/Transport/**`·`Ipc/**`(방금 수정한 영역 — 손대지 않음), 무관한 기존 핸들러, `.asmdef`/`.meta` 파일, Git 파괴 명령.

### 5. Iteration policy (phase-loop 페이즈 = §7 체크리스트 계층)
각 페이즈 완료 후 자기리뷰(메인 모델 Fable 5가 직접 — 전역 에이전트 모델 규칙: 최상위급이면 리뷰 직접 수행) → 검증 게이트 실행 → PASS면 다음 페이즈, FAIL이면 최소 변경 재시도.

- **P1 — 와이어 계약**: `WellKnownCommands`에 `SpatialDescribe`/`SpatialCheck` 추가(Shared + Plugin `Editor/Shared/` 복사본 동기화) + `CommandCatalog` 정의(CLI 이름/파라미터/예시) + schema 카테고리. 게이트: Shared.Tests 3종 guardrail green.
- **P2 — CLI 등록**: `Program.cs`에 `spatial describe`/`spatial check` verb + `Commands/SpatialCommand.cs` + 파서/request 테스트. 게이트: `Cli.Tests --filter Spatial` green.
- **P3 — MCP 표면**: read 명령이므로 `QueryTool` allowlist + schema. 게이트: `Mcp.Tests` green.
- **P4 — Plugin 핸들러**: `SpatialDescribeHandler`/`SpatialCheckHandler`(월드 AABB = Renderer.bounds 합집합 or Collider.bounds; 축 라벨 = size 최소/최대 성분; 법선 = 주요 면 노멀; pivot 편차 = transform.position - bounds.center; 술어 = footprint/overlap/gap 수치). `CommandRegistry` 자동 등록. 게이트: code-patterns 준수 authored + (가능 시) Plugin handler coverage guardrail; Unity 컴파일은 경계로 문서화.
- **P5 — 문서 동기화**: README/getting-started/ai-quickstart/PROJECT-STATUS에 공간 명령 + "스크린샷 대신 측정" 서사 추가. 게이트: 전체 유닛 스위트 green + build 경고 0.

- 패스 예산: 한 페이즈에서 무진전 3패스면 blocked 판정.

### 6. Blocked stop condition
- .NET 계층이 설계 충돌로 green 불가(예: schema guardrail이 결정 요구) → 멈추고 보고.
- Plugin 지오메트리 핸들러가 Unity API 의미상 모호(예: 어떤 bounds 소스가 정답인가) → 4분류로 보고 후 사용자 결정.
- **알려진 경계(블로커 아님)**: Plugin `.cs`는 여기서 `dotnet build` 불가 → "근사(authored, Unity-compile-pending)"로 보고, .NET 게이트 green으로 러닝 A 완료 판정.
- 보고 형식: 재현됨 / 근사됨 / 막힘 / 불확실 4분류.

### 7. 실행 기록 (실행 에이전트가 기록)
- 2026-07-16 Claude Code(Fable 5) — 브리프 작성. phase-loop 실행 대기.
- 2026-07-16 Claude Code(Fable 5) — 러닝 A 실행 완료. phase-loop 재구성: 가드레일(`PluginCommandHandlers_CoverAllTransportCommands`, `CatalogCliNames_AreRegisteredInProgram`, MCP-reachability)이 계층을 교차검증하므로 P1~P3(와이어+CLI+MCP+핸들러)을 한 원자 페이즈로 병합.
  - **재현됨**: `dotnet build unityctl.slnx` 경고 0. 전체 유닛 스위트 green — Shared 105, Core 169, Cli 607(+18 SpatialCommandTests), Mcp 25. 가드레일(카탈로그 스냅샷/핸들러 커버리지/MCP 도달성/CLI 등록) 통과. 신규: `spatial-describe`/`spatial-check` 2종, `SpatialCommand`+테스트, `SpatialGeometryUtility`+2 핸들러, QueryTool/CommandCatalog/WellKnownCommands(×2) 동기화, 문서 3종.
  - **근사됨(경계)**: Plugin `SpatialDescribeHandler`/`SpatialCheckHandler`/`SpatialGeometryUtility`는 Unity API 의존 → 여기서 `dotnet build` 불가. 소스는 code-patterns 준수로 authored·가드레일이 소스 파싱으로 커버리지 검증. 실제 컴파일/런타임은 Unity Editor에서 확인 필요.
  - **막힘**: 없음.
  - **불확실**: 회전된 슬래브의 world AABB 팽창은 true-size(mesh bounds×lossyScale)로 보정하나, MeshFilter 없는 합성 오브젝트는 orientation "unknown"으로 폴백(footprint/gap만 판정). 실 씬에서 검증 권장.
- **다음 러닝**: B(fleet) — 사용자가 최우선으로 지목. 러닝 A의 `spatial check`를 병렬 감사 데모에 사용. 승인 게이트 후 진행.

---

## 러닝 B/C/D — 스케치 (도달 시 확장)

- **B (fleet)**: `unityctl fleet <cmd>` 또는 라우팅 메타로 N 클라이언트 동시 안전 실행. Verified by Core 동시성/라우팅 테스트 green + single-flight 하 충돌 0. 데모: workflow 16-agent 병렬 `spatial check`.
- **C (자율 루프)**: `workflow iterate` = edit→compile-check→playSmoke→(assert 실패 시)screenshot→structured verdict. Verified by verdict 스키마 테스트 + orchestration 단위 테스트.
- **D (CI/headless)**: `.github/workflows/unity-verify.yml` + `unityctl` headless 레시피. Verified by YAML 유효성 + `WorkflowGuardrailTests` green.

## 참조 문서
- `docs/ref/code-patterns.md` §7(새 명령 체크리스트) · §10(응답 크기 규율)
- `docs/status/PROJECT-STATUS.md`
- `CLAUDE.md` 실행 규칙(MUST)
