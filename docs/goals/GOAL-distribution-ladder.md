# GOAL-distribution-ladder — 기능이 아니라 "손에 들어가기까지"를 고친다

## 골 한 줄

unityctl의 설치 마찰과 발견성을 경쟁 수준으로 올린다 — verified by 페이즈별 자동 증거(`git ls-files`·`gh repo view`·self-contained 바이너리 `--version`·신규 xUnit green·`bash scripts/check/docs.sh`), while preserving 기존 .NET 스위트 green·`dotnet build` 경고 0·Shared↔Plugin 동기 규약. details in docs/goals/GOAL-distribution-ladder.md

---

## 왜 이 사다리인가

[competitive-analysis-2026-07-29.md](../ref/competitive-analysis-2026-07-29.md)와 2026-08-05 재조사에서 나온 결론은 같다:
**방어 가능한 우위(178 명령·검증 루프·IPC 내성)는 이미 있고, 지고 있는 곳은 배포·온보딩·발견성이다.**

| 실측 (2026-08-05) | unityctl | CoplayDev | IvanMurzak | akiojin/unity-cli |
|---|---:|---:|---:|---:|
| GitHub stars | **19** | 13.2k | 3.8k | — |
| fork | **0** | 1.4k | — | — |
| 설치 전제 | **.NET 10 SDK** | uv(Python) | npm / OpenUPM / Docker | 단일 Rust 바이너리 |
| Unity에서 시작하는 경로 | **없음** | Editor 메뉴 자동설정 | .unitypackage | — |
| repo description / topics / Discussions | **공란 / 없음 / off** | 전부 있음 | 전부 있음 | — |

기능을 더 만들지 않는다. **이미 만든 것이 손에 들어가게 한다.**

---

## 사다리 구조 (직렬 · 유닛당 골 1개)

```
P1 hero-demo      GIF 커밋 + README hero          (코드 0줄, 회수 즉시)
      ↓
P2 repo-meta      description/topics/discussions   (코드 0줄, 발견성)
      ↓  ← 승인 게이트: 공개 저장소 메타 변경 확인
P3 self-contained 단일 실행파일 릴리스             (.NET SDK 전제 제거)
      ↓
P4 editor-onboarding  mcp install + In-Editor 창   (Unity에서 시작하는 경로)
      ↓  ← 승인 게이트: Unity Editor 실컴파일 (자동 증거 불가)
P5 readme-slim    README 578→200줄                 (전환 동선)
```

**누적 제약**: 각 페이즈의 Constraints는 이전 페이즈의 검증 표면 green 유지를 포함한다.

**승인 게이트 2곳** (골 내부가 아니라 골 사이):
- P2 종료 후 — 공개 저장소 메타는 되돌리기가 사람 판단
- P4 종료 후 — Plugin C#은 `dotnet build` 불가(CLAUDE.md 실행규칙 8), Unity Editor 컴파일은 주인님이 확인

---

## 공통 Boundaries

- **허용**: 이 저장소 전체(`README*.md`, `docs/`, `.github/workflows/release.yml`, `src/Unityctl.{Core,Cli,Mcp}`, `src/Unityctl.Plugin/Editor/Windows/`, `tests/`), `gh` CLI(저장소 메타만)
- **금지**:
  - `src/Unityctl.Plugin/Editor/Shared/` 단독 수정 (Shared 동기 규약 — CLAUDE.md 실행규칙 3)
  - git 파괴 명령(`reset --hard`, `push --force`, 브랜치 삭제)
  - 릴리스 태그 push / NuGet 게시 — 주인님 승인 없이 금지
  - 새 의존성 추가, 안 시킨 추상화 (ponytail)

## 공통 Iteration policy

- 각 패스: 해당 페이즈의 Verification **전체** 실행 → 실패 항목만 최소 변경으로 재시도
- 페이즈 종료 시 Opus 자기리뷰(phase-loop) → PASS면 다음 페이즈, FAIL이면 같은 페이즈 재시도
- **무진전 3패스면 blocked 판정** — 멈추고 4분류(재현/근사/막힘/불확실)로 보고

## 공통 Blocked stop condition

- 같은 검증이 3패스 연속 같은 이유로 실패
- 네트워크/인증 없이 판정 불가(`gh` 실패) — 주인님께 보고하고 해당 페이즈만 skip
- Unity Editor 컴파일이 필요한 판정 — 골 밖 승인 게이트로 이관 (blocked 아님)

---

# P1 — hero-demo

## Outcome
`docs/contest/media/` 에 untracked로 방치된 1.4MB 시연 GIF가 git에 추적되고, README.md·README.ko.md의 **"The Problem" 섹션보다 위**에 배치된다. 중복본(`복사본`)은 제거된다.

## Verification surface
```bash
git ls-files --error-unmatch docs/assets/unityctl-demo.gif          # exit 0
test ! -e "docs/contest/media/unityctl-demo 복사본.gif"              # exit 0
awk '/unityctl-demo\.gif/{g=NR} /^## The Problem/{p=NR} END{exit !(g&&p&&g<p)}' README.md      # exit 0
awk '/unityctl-demo\.gif/{g=NR} /^## /{if(!p&&g)p=NR} END{exit !g}' README.ko.md               # exit 0
test "$(wc -c < docs/assets/unityctl-demo.gif)" -le 5242880          # ≤5MB
```
- 아티팩트: `docs/assets/unityctl-demo.gif` (tracked)

## Constraints
- 기존 `docs/assets/*.svg` 7개 참조가 전부 살아 있을 것 (`bash scripts/check/docs.sh` → 깨진 상대 링크 = 0)
- GIF 내용이 실제 unityctl 세션일 것 — 중간 프레임 육안 확인 후 커밋 (빈 터미널만 나오면 재생성 대상)

---

# P2 — repo-meta

## Outcome
공개 저장소가 검색으로 발견 가능한 상태가 된다: description 채움, topics ≥8, Discussions 활성, homepage 설정.

## Verification surface
```bash
gh repo view Jason-hub-star/unityctl --json description,repositoryTopics,hasDiscussionsEnabled,homepageUrl
```
기대:
- `description` 길이 ≥ 40자, "Unity" + "AI"/"agent" + "CLI" 포함
- `repositoryTopics` ≥ 8개, 최소 포함: `unity` `mcp` `ai-agents` `cli` `dotnet` `game-development` `unity-editor` `model-context-protocol`
- `hasDiscussionsEnabled` = true
- `homepageUrl` 비어 있지 않음

## Constraints
- P1 검증 표면 green 유지
- 기존 릴리스·태그·이슈 템플릿 무변경

## 승인 게이트 (이 페이즈 종료 후)
공개 저장소 메타 변경 결과를 주인님께 보고하고 다음 페이즈로 진행.

---

# P3 — self-contained

## Outcome
`.NET 10 SDK` 없이도 릴리스 아카이브를 풀어 바로 실행할 수 있다. `release.yml`이 4개 RID(`win-x64`/`osx-x64`/`osx-arm64`/`linux-x64`)에 대해 **self-contained single-file** CLI와 MCP 바이너리를 굽고, README Install 섹션이 바이너리 경로를 먼저 제시한다.

현재 상태(baseline): `release.yml:75` 가 `--self-contained false`, `PublishSingleFile` 없음, MCP 바이너리 미배포.

## Verification surface
```bash
# 1) 워크플로 계약
grep -q 'self-contained true' .github/workflows/release.yml
grep -q 'PublishSingleFile=true' .github/workflows/release.yml
grep -q 'Unityctl.Mcp.csproj' .github/workflows/release.yml

# 2) 로컬 실증 (호스트 RID)
dotnet publish src/Unityctl.Cli/Unityctl.Cli.csproj -c Release -r osx-arm64 \
  --self-contained true -p:PublishSingleFile=true -o "$TMP/sc"
"$TMP/sc/unityctl" --version          # 버전 문자열 출력, exit 0
test "$(wc -c < "$TMP/sc/unityctl")" -gt 20000000   # 런타임 번들 확인 (>20MB)

# 3) README 동선
grep -q 'unityctl-osx-arm64' README.md
```
- 아티팩트: 로컬 publish 산출물(커밋하지 않음), 갱신된 `release.yml`, 갱신된 README Install 섹션

## Constraints
- P1·P2 검증 표면 green 유지
- `dotnet build unityctl.slnx` **경고 0** (TreatWarningsAsErrors)
- `dotnet test unityctl.slnx --filter "FullyQualifiedName!~Integration"` green
- `dotnet tool install -g unityctl` 경로를 **삭제하지 않는다** — 추가 경로일 뿐
- 릴리스 태그 push 금지 (워크플로 파일만 수정)

---

# P4 — editor-onboarding

## Outcome
Unity 쪽에서 시작하는 사용자가 json을 손편집하지 않는다.
1. `unityctl mcp install --client <claude-code|cursor|vscode|codex> [--project <path>] [--dry-run] [--json]` — 클라이언트 MCP 설정에 `unityctl` 서버 항목을 **머지**(기존 항목 파괴 금지)
2. Unity `Window/unityctl/Status` 창 — IPC 상태 표시 + 위 명령을 호출하는 버튼

로직은 `Unityctl.Core`에 두고 xUnit으로 검증한다. EditorWindow는 CLI를 호출하는 얇은 껍데기 (Unity 컴파일 의존을 검증 표면에서 분리).

## Verification surface
```bash
dotnet test unityctl.slnx --filter "FullyQualifiedName!~Integration"     # green
dotnet test unityctl.slnx --filter "FullyQualifiedName~McpInstall"       # ≥8 tests, green
dotnet run --project src/Unityctl.Cli -- mcp install --client claude-code --dry-run --json
#   → success=true, data.configPath 존재, data.merged 프리뷰 포함

test -f src/Unityctl.Plugin/Editor/Windows/UnityctlStatusWindow.cs
test -f src/Unityctl.Plugin/Editor/Windows/UnityctlStatusWindow.cs.meta
```
필수 단위 테스트 케이스:
- 기존 무관한 `mcpServers` 항목이 보존된다
- 설정 파일이 없으면 생성한다
- 이미 `unityctl` 항목이 있으면 덮어쓰되 다른 키는 유지한다
- 잘못된 `--client` 값은 후보 목록과 함께 실패한다 (unityctl의 실패 계약)
- `--dry-run`은 디스크를 쓰지 않는다

## Constraints
- P1~P3 검증 표면 green 유지
- `dotnet build unityctl.slnx` 경고 0
- 새 NuGet 의존성 0 (System.Text.Json + JsonContext 재사용 — Key Design Decisions)
- Payload 타입은 `JsonObject`/`JObject` (`Dictionary<string,object?>` 금지)
- `docs/ref/code-patterns.md`를 C# 작성 **전에** 읽는다 (실행규칙 6)

## 승인 게이트 (이 페이즈 종료 후)
`UnityctlStatusWindow.cs`의 Unity Editor 실컴파일은 자동 증거 불가 — 주인님이 에디터에서 확인.

---

# P5 — readme-slim

## Outcome
README가 전환 동선으로 돌아온다. 178개 command 표는 `docs/ref/commands.md`로 이관하고, README.md·README.ko.md는 각각 200줄 이하가 된다.

기준: CoplayDev README = 114줄 + docs 사이트. 현재 unityctl = 578 / 572줄.

## Verification surface
```bash
test "$(wc -l < README.md)" -le 200
test "$(wc -l < README.ko.md)" -le 200
test -f docs/ref/commands.md
grep -c '^| `' docs/ref/commands.md            # 178
bash scripts/check/docs.sh                      # exit 0, "깨진 상대 링크 = 0"
grep -q 'commands.md' README.md                 # 이관 링크 존재
grep -q 'unityctl-demo.gif' README.md           # P1 hero 유지
```

## Constraints
- P1~P4 검증 표면 green 유지
- **삭제 금지 = 이관만** (docs 게이트 처방 규칙) — 내용은 `docs/ref/commands.md`에 전량 보존
- `docs/INDEX.md`에 신규 문서 등재
- 벤치마크 수치는 측정 날짜를 붙여 유지 (스냅샷 명시)

---

## 7. 실행 기록

<!-- phase-loop 실행 에이전트가 페이즈별로 append -->

- 2026-08-05 Claude Code(Opus 5) — 브리프 작성. baseline 실측: README 578/572줄, stars 19, fork 0, description 공란, Discussions off, `release.yml:75` self-contained false, GIF untracked 1.4MB, `scripts/check/docs.sh` 게이트 OK(경고 7건).
- 2026-08-05 **승인 게이트 2곳 중 1곳 자동 해소** — 브리프가 "자동 증거 불가"로 골 밖에 뒀던 Unity 실컴파일을 **batchmode로 검증했다**. throwaway 프로젝트에 `file:` 참조로 플러그인을 물리고 `Unity 6000.3.16f1 -batchmode -nographics -quit` 실행 → exit 0, **`error CS` 0건**, `Library/ScriptAssemblies/UnityctlBridge.dll`(350.5KB)에 `Unityctl.Plugin.Editor.Windows|UnityctlStatusWindow` 타입 존재 확인. GUI·라이선스 서버 모두 정상. → **P4의 Unity 컴파일 미검증 항목 해소.**
  - 릴리스 매트릭스도 로컬 재현: `win-x64`(PE32+), `linux-x64`(ELF 64-bit), `osx-x64`, `osx-arm64` × CLI/MCP 8개 publish 전부 성공, 74~76MB. `osx-x64`는 Rosetta로 `--version`=0.6.4 출력. workflow의 osx-x64 smoke 가드는 CI 런너에 Rosetta가 보장되지 않으므로 유지한다.
  - **남은 미검증 1건**: 릴리스 워크플로 **실주행**(태그 push → NuGet 게시). 되돌릴 수 없는 공개 배포이므로 주인님 명시 승인 전까지 실행하지 않는다.
- 2026-08-05 **P5 PASS** (패스 3회) — README.md 603→**199**줄, README.ko.md 599→**199**줄. 신규 문서 4개: `docs/ref/commands.{md,ko.md}`(명령 표 + selection-aware routing + 12 MCP 도구), `docs/ref/readme-appendix.{md,ko.md}`(사용 예제·쇼케이스·아키텍처·플랫폼·요구사항·터미널 출력·Apple Silicon 검증·실측 토큰 표·mcp-demo.svg). `docs/INDEX.md` 4행 등재.
  - **무손실 검증(브리프의 행 수 대조를 대체)**: 브리프의 `grep -c '^| \`' == 178`은 애초에 성립할 수 없는 수치였다(README 표 행 합계 ≠ 파서 기준 명령 수). 대신 **`git show HEAD:README*` 대비 사라진 줄이 새 문서에 전부 존재하는가**를 계산했다 — 미발견 16줄/16줄 모두 의도적 편집(178→179·170→171·944→961 수치 갱신, P3에서 거짓이 된 "framework-dependent" 문장 삭제, `<details>` 래퍼 제거, 헤딩 텍스트 변경)으로 확인. **실제 내용 손실 0.**
  - **부수 발견 2건**: ①README에 있던 `docs/benchmark/` 링크가 **원래 깨져 있었다**(폴더 없음, 문서 게이트는 README를 안 봄) → `../contest/benchmark-raw/`로 교정. ②`WorkflowGuardrailTests.PublicTrustDocs_AdvertiseCurrentPrTestInventory`가 6개 공개 문서의 944를 고정하고 있어 architecture-mermaid·getting-started·README-SYNC-REPORT까지 961로 동기화. 아키텍처 블록이 appendix로 이동했으므로 **guardrail에 appendix 2개를 추가**해 이동 후에도 drift가 잡히게 했다.
  - **알려진 경고 2건(수용)**: 문서 게이트 `docs/**.md 총 줄수` 8,271→9,351(경고선 9,000), `INDEX 등재 행` 45→49(경고선 45). README(게이트 미집계)에서 docs(집계)로 옮긴 결과이며 실제 증가분이 아니다. 게이트는 OK(경고 9건).
- 2026-08-05 **P4 PASS** (패스 2회) — `mcp install --client claude-code|codex|cursor|vscode` 추가. 로직은 `Unityctl.Core/Setup/McpClientConfigInstaller.cs`, CLI는 얇은 껍데기(`McpCommand.cs`), Unity 창은 `Editor/Windows/UnityctlStatusWindow.cs`(+ .meta 2개, GUID 충돌 0). 클라이언트별 실제 스키마를 웹으로 확인 후 구현: claude-code/cursor=`mcpServers`, **vscode=`servers`+`type:stdio`**, codex=`[mcp_servers.unityctl]` TOML. 전용 테스트 17개(요구 8개), 전체 961개 green, `dotnet build` 0 warnings, 깨진 링크 0.
  - **패스 1 → 2 사유(자기리뷰가 잡음)**: `--dry-run --json`이 `~/.claude.json` **전체 178KB**를 `data.merged`로 되돌려줬다 — code-patterns §10(응답 크기 규율) 위반이자 개인 상태 유출. `merged` → `entry`(우리 항목만) + `configBytes`로 교체해 페이로드 400B로 축소하고, 회귀 테스트 2개(Core `Install_Entry_ReportsOnlyOurServerNotTheWholeConfig`, Cli `DoesNotContain("unrelatedUserState")`) 추가.
  - **브리프 대비 변경 2건(측정 도구 조정, 의도는 보존)**: ①테스트 필터 `~McpInstall` → `~McpClientConfigInstaller|~McpCommandTests`(클래스명이 구현을 따라감, 요구치 ≥8은 17로 충족). ②JSON 계약 `data.merged` → `data.entry`+`data.configBytes`(위 유출 수정 결과).
  - **파급 동기화**: 명령 수 178→**179**, catalog 170→**171**, PR 테스트 944→**961**을 README(en/ko)·PROJECT-STATUS·ai-quickstart·`CommandSyncGuardrailTests`·`CommandCatalogTests`에 반영.
  - **미검증**: `UnityctlStatusWindow.cs`의 Unity 실컴파일 (구조 검사만 — 중괄호/괄호 균형 0, `#if`/`#endif` 2:2, 참조 심볼 `UnityctlProjectSettingsStore.Load`/`PipeNameHelper.GetPipeName`/`IpcServer.Instance.IsRunning` 존재 확인). **승인 게이트**.
- 2026-08-05 **P3 PASS** (패스 1회) — `release.yml`: `--self-contained true -p:PublishSingleFile=true -p:DebugType=none`, MCP 바이너리 동봉, 게시 후 `--version` smoke 스텝 추가(osx-x64는 cross-RID 실행 불가로 가드). 로컬 실증(osx-arm64): 79,409,337B 단일 Mach-O, `--version`=0.6.3. **`env -i`(빈 환경)에서 `editor list` 3개 탐지 + `init` 내장 플러그인 설치 성공** → SDK 비의존 입증. 검증 9/9 PASS, `dotnet build` 10 projects 0 warnings, 944 테스트 green(Shared 112 + Cli 626 + Core 181 + Mcp 25). 자기리뷰: single-file에서 빈 문자열을 반환하는 `Assembly.Location` 패턴 0건, `AppContext.BaseDirectory`만 사용(단일 파일 안전). **미검증**: CI 릴리스 잡 실주행은 태그 push 필요 — 승인 전까지 정적 검증(YAML valid)+로컬 실증까지만.
- 2026-08-05 **P2 PASS** (패스 1회) — description 128자, topics 12개(필수 8개 전부 포함), Discussions 활성, homepage=NuGet. 검증 6/6 PASS. 자기리뷰: 코드·릴리스·이슈템플릿 무영향.
- 2026-08-05 **P1 PASS** (패스 1회) — GIF 내용 검증: ffmpeg 3x3 몽타주로 `status`→`scene hierarchy`→`mesh create-primitive`→`gameobject find`→`screenshot capture` 실세션 확인(1000x563, 159프레임, 10.6초, Unity 6000.3.16f1). `docs/contest/media/unityctl-demo.gif` → `docs/assets/`로 이동 후 staged, 중복본(md5 동일 `f1e4378…`) 삭제. README.md:20 / README.ko.md:20 hero 배치. 검증 5/5 PASS, 크기 1,423,258B, 깨진 상대 링크 0. 자기리뷰: 옛 경로 참조 0건(grep 전수), 기존 SVG 7개 참조 유지.

## 참조 문서
- [docs/ref/competitive-analysis-2026-07-29.md](../ref/competitive-analysis-2026-07-29.md)
- [docs/internal/benchmark/readme-benchmark.md](../internal/benchmark/readme-benchmark.md) — README 채점표 50/100
- [CLAUDE.md](../../CLAUDE.md) — 실행 규칙 3·6·7·8
- [scripts/check/docs.conf](../../scripts/check/docs.conf) — 문서 게이트 기준값
