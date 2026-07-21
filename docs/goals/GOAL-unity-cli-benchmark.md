# GOAL-unity-cli-benchmark — 공식 Unity CLI 대응 사다리 (벤치마크 → 흡수 → 포지셔닝)

## 골 한 줄
공식 Unity CLI(com.unity.pipeline) 대비 8태스크 벤치마크 16셀 완성으로 흡수 우선순위를 측정으로 확정 — verified by `docs/contest/benchmark-vs-unity-cli.md` 완성 + `dotnet test unityctl.slnx --filter "FullyQualifiedName!~Integration"` green 유지(src 무변경), while preserving SampleUnityProject 원본 불변. details in docs/goals/GOAL-unity-cli-benchmark.md
(Codex 투입 시: 앞에 `/goal ` 접두)

---

## 골 사다리 (직렬, 유닛당 골 1개 — 승인 게이트는 러닝 사이)

이 초기 브리프는 **러닝 A(비교 벤치마크)** 만 6요소 완전 명세한다. B~F는 아웃컴+검증 표면만 스케치하고, A 완료 후 승인 게이트에서 C/D/E의 우선순위·순서를 벤치마크 수치로 확정하며 이 문서를 확장한다.

| 러닝 | 유닛 | 왜 이 순서 |
|---|---|---|
| **A (지금)** | 공식 CLI 비교 벤치마크 (8태스크 × 2도구 = 16셀) | 흡수 우선순위를 감이 아닌 측정으로 결정. src 무변경 → 리스크 0. experimental 0.3의 실동작 검증 자체가 데이터 |
| **B** | 공모전 포지셔닝 문서 v1 (`docs/contest/`) | 벤치마크 수치가 바로 재료. 공모전 마감이 코드 작업보다 급함 |
| **C** | Roslyn eval (opt-in 게이트 + 가드레일) | 격차 1위 후보 — 공식 `eval`은 풀 C#, 우리 `exec`는 리플렉션 단일 표현식. A 결과로 순위 확정 |
| **D** | 플레이어 런타임 제어 (IPC 런타임 컴포넌트) | 격차 2위 후보 — dev build status/logs. 데스크톱 Named Pipe 우선 |
| **E** | 명령 커버리지 갭 (occlusion / Timeline / AnimatorController / input sim) | 기계적 7계층 추가 — 벤치마크에서 드러난 격차 순 |
| **F** | 통합 검증 + 포지셔닝 v2 | 흡수 후 벤치마크 재실행으로 격차 축소를 수치로 증명, 사다리 마감 |

**승인 게이트**: A→B 사이(흡수 우선순위 C/D/E 순서 확정 — 사람 판단), B→C 사이(공모전 반영 확인).
**누적 제약**: 각 러닝의 Constraints에 "이전 러닝의 검증 표면 green 유지"를 누적한다.

---

## 러닝 A — 공식 CLI 비교 벤치마크

### 1. Outcome (측정 가능한 완료 상태)
- `docs/contest/benchmark-vs-unity-cli.md` 존재, **8태스크 × 2도구 = 16셀** 완성. 각 셀: 성공/부분/실패 판정 + 레이턴시(ms) + 응답 크기(bytes, 토큰 프록시) + 재현 명령.
- 8태스크 (동일 프로젝트 사본에서 양쪽 실행):
  1. scene hierarchy 읽기
  2. GameObject 생성 + component set-property
  3. prefab instantiate
  4. play mode 진입 + 콘솔 수집
  5. 스크린샷 캡처
  6. 테스트 실행 (EditMode)
  7. 라이브 코드 실행 — 공식 `eval` vs 우리 `exec` (동일 로직: 씬 MeshRenderer 집계 + 속성 1개 변경)
  8. 도메인 리로드 유발 후 재연결 내성 (리로드 중/직후 명령 성공 여부)
- 환경 부록: unity CLI 버전 / com.unity.pipeline 버전 / unityctl 버전·커밋 / Unity 6000.0.64f1 / macOS.
- **공식 도구의 실패도 데이터로 기록** (experimental 실동작 검증이 목적의 일부) — blocked 아님.
- unityctl 버그 발견 시 `docs/status/CTL-FEEDBACK.md`에 기록만 (이 골에서 수정하지 않음).

### 2. Verification surface (실행 에이전트가 직접 실행)
- 명령: `test -f docs/contest/benchmark-vs-unity-cli.md && grep -c '^| T[1-8]' docs/contest/benchmark-vs-unity-cli.md` → 기대: 8 이상 (태스크 행 완성).
- 명령: `git diff --stat -- src/` → 기대: 빈 출력 (제품 코드 무변경).
- 명령: `git status --porcelain tests/Unityctl.Integration/SampleUnityProject/ | grep -v '^??'` → 기대: 빈 출력 (원본 무오염 — 단 기존 dirty 파일은 착수 시점 스냅샷과 대조).
- 명령: `dotnet test unityctl.slnx --filter "FullyQualifiedName!~Integration"` → 기대: 전체 green (908+).
- 아티팩트: 벤치마크 문서 + raw transcript 부록 (`docs/contest/benchmark-raw/` 또는 문서 내 부록 섹션).

### 3. Constraints (후퇴 금지)
- `src/**` 제품 코드 무변경 — 이 골은 측정이지 수정이 아니다.
- SampleUnityProject **원본** 무오염 — 공식 패키지 추가는 스크래치 사본에서만.
- 기존 유닛 스위트 green 유지.
- 측정 공정성: 동일 태스크·동일 프로젝트 상태·각 도구의 native 권장 방식 사용 (한쪽에 유리한 우회 경로 금지).

### 4. Boundaries
- 허용: `docs/contest/**`, `docs/goals/**`, `docs/status/**`(기록), 스크래치 디렉터리(프로젝트 사본 + 공식 CLI 설치), `tests/Unityctl.Integration/SampleUnityProject/**` **읽기**.
- 금지: `src/**` 수정, SampleUnityProject 원본 수정(`Packages/manifest.json` 포함), Git 파괴 명령.

### 5. Iteration policy
각 패스: 검증 게이트 실행 → 실패 항목만 최소 변경 재시도. 무진전 3패스면 blocked.

- **P1 — 환경**: 공식 CLI 설치(beta 채널 스크립트) + `unity --version` 확인 → **[사용자 개입 게이트: `unity auth login` 브라우저 로그인]** → SampleUnityProject 사본 생성(스크래치) + com.unity.pipeline 추가 + unityctl 플러그인 설치 + 에디터 기동. 게이트: 양쪽 브릿지 모두 ping 성공.
- **P2 — unityctl 측정**: 사본에서 8태스크 실행 + 기록. 게이트: unityctl 열 8행 완성.
- **P3 — 공식 측정**: 동일 사본·동일 상태에서 8태스크 실행 + 기록. 게이트: 공식 열 8행 완성.
- **P4 — 문서화**: 비교 표 + 분석(격차 순위 제안 포함) + 환경 부록. 게이트: §2 Verification 전체 green.

### 6. Blocked stop condition
- `unity auth login` 수행 불가(사용자 부재·계정 문제) → 멈추고 보고.
- 공식 CLI 설치 스크립트가 macOS에서 2회 재시도 후에도 실패 → 멈추고 보고.
- com.unity.pipeline이 6000.0.64f1에서 설치 자체가 불가 → **blocked 아님**: "공식 N/A + 판정 근거"로 기록하고 unityctl 단독 열로 벤치마크 완료 (그 자체가 공모전 데이터).
- 보고 형식: 재현됨 / 근사됨 / 막힘 / 불확실 4분류.

---

## 러닝 B~F 스케치 (해당 러닝 도달 시 6요소로 확장)

### B — 공모전 포지셔닝 문서 v1
- Outcome: `docs/contest/positioning-vs-unity-cli.md` — 벤치마크 수치 인용 + 차별점(spatial grounding / workflow verify / 토큰 규율 / 2021.3+ 지원 / no-auth 로컬) + 흡수 로드맵(C~E).
- Verification: 파일 존재 + 인용 수치가 벤치마크 문서와 일치(grep 대조). 결과보고서(.docx/.pages) 반영은 골 밖 사용자 승인 게이트.

### C — P0 브릿지 수명주기 + P1 Roslyn eval (2026-07-21 게이트 승인으로 확장)

**C-P0 (브릿지 수명주기, 벤치마크 T8 근본 원인 수정)**
1. Outcome: 무인(비포커스·화면잠금) 에디터에서 브릿지가 부팅·도메인 리로드 후 모두 자동 기동. 근본 원인 = `EditorApplication.delayCall`은 리페인트에 묶여 무인에서 안 흐름(update는 흐름 — 벤치마크에서 명령 실행 펌프 정상 동작으로 실증). 플러그인 내 delayCall 4개소(Bootstrap 시작 / IpcServer watch 구독 / AssetRefreshHandler 이중 defer) 전부 update-기반 디스패치로 교체.
2. Verification: `dotnet build unityctl.slnx` 경고 0 + 전체 유닛 스위트 green(920) + **라이브 재현**: 벤치 사본 에디터를 무인 재기동 → 포커스/킥 없이 `ipc-state.json` ready + ping IPC 성공 → `script create`로 리로드 유발 → 리로드 후 명령 성공(T8 시나리오 역전).
3. Constraints: 기존 스위트 green, batchmode 가드 유지, ready-게이트(isCompiling/isUpdating) 의미 보존, `.asmdef`/`.meta` 수동 수정 금지.
4. Boundaries: `src/Unityctl.Plugin/Editor/{Bootstrap,Ipc,Commands,Utilities}/**`, `docs/**`. 금지: Shared 프로토콜 변경, Core/Cli 변경.
5. Iteration: 패치 → dotnet build/test → 라이브 재현 → 실패 항목만 재시도, 무진전 3패스 blocked.
6. Blocked stop: delayCall 교체 후에도 무인 기동 실패(원인이 다른 층)면 측정 로그 첨부 후 보고.

**C-P1 (Roslyn eval, opt-in)**
- Outcome: 다중 문장 C# 실행(가칭 `exec eval`) — 기본 off, 프로젝트 설정 opt-in으로만 활성. 기존 `exec` 표현식 경로 무변경. 구현 전략은 외부 Roslyn DLL 동봉 대신 Unity 동봉 csc로 임시 어셈블리 컴파일→로드→실행(의존성 0) 우선 검토.
- Verification: 7계층 guardrail + 신규 유닛 테스트 green + 라이브 transcript(벤치마크 T7b 시나리오 성공). 누적: C-P0 검증 표면 green 유지.

### D — 플레이어 런타임 제어 (다음 러닝 — 착수 시 상세)
1. Outcome: Development Build 플레이어에 런타임 브릿지 — 최소 `runtime status`/`runtime logs` 2명령. 데스크톱 Named Pipe, 파이프명/디스커버리는 상태 파일(`Application.persistentDataPath/unityctl-runtime.json`) 방식. 릴리스 빌드에는 절대 미포함(`Debug.isDebugBuild` + define 게이트).
2. Verification: SampleUnityProject dev 플레이어 빌드(`unityctl build`) → 실행 중 플레이어에 `unityctl runtime status --state-file <path>` 응답 transcript + 신규 유닛 테스트 green.
3. Constraints: 기존 Editor asmdef(`UnityctlBridge.asmdef`) 무수정 — Runtime은 **신규** asmdef로 분리. Editor IPC 경로 무변경. 이전 러닝 검증 표면(A~C·E) green 누적.
4. Boundaries: `src/Unityctl.Plugin/Runtime/**`(신규), `src/Unityctl.{Shared,Core,Cli}/**`(runtime 타게팅), `tests/**`, `docs/**`.
5. Iteration: 설계(프레이밍/디스커버리 재사용 범위) → Runtime 서버 → CLI 타게팅 → dev 빌드 라이브 검증. 무진전 3패스 blocked.
6. Blocked stop: Mono 플레이어에서 Named Pipe 미지원 판명 시 TCP 폴백 설계로 전환 보고.

**D 완료 (2026-07-21). 판정: 재현됨 (라이브 검증)**
- Runtime 신규 asmdef(`UnityctlBridge.Runtime`) + `RuntimeBridge`(RuntimeInitializeOnLoad, `!isEditor && isDebugBuild` 게이트, DontDestroyOnLoad 펌프) + `RuntimePipeServer`(에디터와 동일 와이어 계약 — LE framing + CommandResponse 형태 → CLI 클라이언트 무변경 재사용). 로그 링버퍼 200개.
- 디스커버리: `persistentDataPath/unityctl-runtime.json` + Player.log에 경로 출력. CLI `runtime status`/`runtime logs --state-file` — `IpcTransport(pipeName, useRawPipeName:true)` internal 생성자 재사용. runtime-* 명령은 의도적으로 WellKnownCommands 밖(에디터 transport 표면 아님) — `CliCommandSuggestions` 명시 목록에 등재(play 동사 선례).
- 라이브: dev 플레이어를 **exec eval로 빌드**(BuildPipeline+BuildOptions.Development — build 명령에 dev 플래그 부재 확인, 백로그감) → 실행 중 플레이어에서 status(scene/playTime 159s/fps 30) + logs(브릿지 기동 로그) 응답. 샘플 프로젝트 테스트 asmdef가 링커 nunit 미해결로 빌드를 깨는 것 확인(스크래치에서 Assets/Tests 제거로 해소 — 제품 이슈 아님).
- 게이트: build 경고 0 / 전체 유닛 927 green(신규 runtime 테스트 6) / meta 가드레일 green / getting-started Runtime 섹션 추가.
- 잔여: F(재벤치마크 + 포지셔닝 v2). 백로그: `build --development` 플래그, runtime input-sim/hot-reload(공식 대비 후순위), 강제 종료된 플레이어의 stale state 파일(클라이언트 pid 생존 확인으로 보완 가능).

### E — 명령 커버리지 갭
- Outcome: occlusion bake / Timeline / AnimatorController 저작 / input simulation 중 벤치마크 격차 순 구현 (7계층 동기화).
- Verification: 7계층 guardrail + 신규 테스트 green.
- **E-P2/P3 완료 (2026-07-21, C→D/E 게이트 승인 후 우선 실행)**:
  - P2 리졸버 통일: `GlobalObjectIdResolver.ResolveGameObject` 신설 — GlobalObjectId → GameObject.Find → 전 로드 씬 경로/이름 DFS(비활성 포함). 핸들러 18파일 19호출처 일괄 전환. 라이브: `prefab create --target <이름>` 성공(벤치마크 실패 셀 역전), `gameobject delete --id <이름>` 성공.
  - P3 친화 속성명: `SerializedPropertyResolver.FindFlexible`(exact → m_PascalCase → top-level 대소문자 무시 스캔) + 실패 시 후보 목록(20개 캡). component/scriptableobject set-property 적용. 라이브: `--property mass` → `m_Mass` 해석·설정 성공, 미존재 속성은 후보 14개 목록 반환.
  - 게이트: build 경고 0 / 전체 유닛 921 green / guardrail green(meta 포함) / getting-started 노트 추가.
  - 잔여 E 항목(occlusion/Timeline/AnimatorController/input sim)은 벤치마크 직접 격차 증거가 없어 후순위 — 러닝 F 재벤치마크 후 필요 시.

### F — 통합 검증 + 포지셔닝 v2
- Outcome: 흡수 후 벤치마크 **재실행** — 격차 축소를 수치로 갱신, 포지셔닝 문서 v2 + 문서 스위트(PROJECT-STATUS/README) 동기화.
- Verification: 전체 스위트 green + 갱신된 벤치마크 16셀 + 문서 대조.

**F 완료 (2026-07-21) — 사다리 완주. 판정: 재현됨**
- V2 재측정 9/9 성공(전부 무인): T8 141,656ms 실패→313~516ms / T7b 거부→1,755ms(공식 2,634ms보다 33% 빠름) / T2c mass 첫 시도 358ms / T3a 이름 1콜 415ms / T1 회귀 없음 / 브릿지 무킥 자동 기동.
- 문서 동기화: benchmark 문서 v2 섹션 + positioning v2 + PROJECT-STATUS(슬라이스·집계 86 allowlist/163 CLI/927 tests) + CLAUDE.md(현재 상태·최근 확정 롤링) + README/README.ko(exec eval·runtime 행) + raw ndjson 갱신.
- 게이트: 전체 유닛 927 green. 사다리 A~F 전 러닝 종료. 골 밖 잔여: 결과보고서(.docx/.pages)·§6.5 비교표·접수 카피 반영(사용자 승인), 커밋 정리(사용자 결정), 백로그(build --development, runtime hot-reload/input-sim, stale state 파일 pid 체크).

---

## 7. 실행 기록 (실행 에이전트가 기록)
- 2026-07-21 Claude Code (Fable 5) — 러닝 A 완료. **판정: 재현됨 (16셀 전부 측정)**.
  - P1: 공식 CLI 1.0.0-beta.2 설치, auth는 Hub 자격증명 공유로 자동 통과(사용자 개입 0회). 사본 + com.unity.pipeline 0.3.1-exp.1 + unityctl 플러그인 동거, 양쪽 ping 성공. 브릿지 활성화에 `ProjectSettings/UnityctlSettings.asset` 수동 생성 필요했음(`unityctl init`은 file:-설치와 충돌).
  - P2/P3: 8태스크 × 2도구 완료. 공식 인자 문법은 `--param value` 플래그식(문서 부재로 PackageCache README에서 확인, key=value는 조용히 무시됨 — 가짜 성공 유발).
  - 핵심 발견: ① unityctl 읽기/쓰기 왕복 우위(T1 2.2배, T4 2.7배) ② Roslyn eval 격차 실증(T7b) ③ **P0 버그 — 무인 에디터에서 브릿지 기동/재기동 실패**(delayCall 기아, 141s 타임아웃; 공식은 static-ctor로 생존) → CTL-FEEDBACK 등재 ④ 공식도 무인 cold에서 run_tests 0개 가짜 성공.
  - P4 검증 게이트: 표 8행 ✓ / src 무변경 ✓(diff 4파일은 세션 이전 브랜치 변경분과 일치) / 원본 무오염 ✓(packages-lock.json도 이전 변경분) / 유닛 스위트 green ✓ (Shared 109 + Core 169 + Cli 617 + Mcp 25 = 920).
  - 산출물: `docs/contest/benchmark-vs-unity-cli.md`, `docs/contest/benchmark-raw/results.ndjson`, CTL-FEEDBACK 항목 1건.
  - 승인 게이트 대기: 흡수 우선순위 확정 — 측정 근거로 **P0 브릿지 수명주기 재설계**를 Roslyn eval보다 앞세울 것을 제안 (벤치마크 문서 "흡수 우선순위 제안" 참조).
- 2026-07-21 Claude Code (Fable 5) — A→B 게이트 승인(사용자 "ㄱ", 제안 순위 수용). **러닝 B 완료. 판정: 재현됨**.
  - 산출물: `docs/contest/positioning-vs-unity-cli.md` — 실측 수치 인용 포지셔닝(한 줄 메시지 / 16셀 발췌 표 / "공식이 있는데 왜 필요한가" 6논거 / 흡수 로드맵 P0~P4 / §6.5 서사 연결).
  - 검증: 인용 수치 grep 대조 전부 벤치마크 문서와 일치 (286/919/617/1602/965/2588/2634/739/141.7s/버전/커밋; 25%는 원수치 5150·6867에서 파생).
  - 골 밖 승인 대기: 결과보고서(.docx/.pages)·§6.5 비교표 열 추가·접수 카피 반영은 사용자 승인 후. B→C 게이트: 러닝 C(P0 브릿지 수명주기 + Roslyn eval) 착수 승인.
- 2026-07-21 Claude Code (Fable 5) — B→C 게이트 승인(사용자 "ㄱ"). **러닝 C 완료. 판정: 재현됨 (P0·P1 모두 라이브 검증)**.
  - **C-P0 브릿지 수명주기**: 진범 = delayCall(리페인트 묶임, 무인에서 불류) vs update(무인에서도 틱). `MainThreadDispatch.RunDeferred` 신설(update 기반), delayCall 4개소 교체(Bootstrap 직접 ScheduleStart / IpcServer watch 구독 / AssetRefreshHandler 2틱 defer). 라이브: 무인 부팅 ready 10초 내(수정 전 무한 대기), 리로드 후 자동 재기동(stop→start 로그 쌍), T8 141,656ms 실패→252ms 성공. 무인 asset refresh도 실전 검증(meta 생성 트리거).
  - **C-P1 exec eval**: `exec-eval` 7계층 추가(WKC Shared+Plugin 동기화 / Catalog+All / CLI `exec eval` verb+`ExecCommand.Eval` / MCP RunTool allowlist / `ExecEvalHandler` — Unity 동봉 csc(DotNetSdkRoslyn/csc.dll + NetCoreRuntime/dotnet)로 임시 어셈블리 컴파일→로드→실행, 외부 의존성 0). opt-in: `UnityctlSettingsData.AllowEval`(기본 false, 게이트 메시지 검증). 라이브: T7b 시나리오 성공(result 2, compile 688ms + execute 1ms = 977ms; 공식 eval 2634ms 대비 2.7배 빠름).
  - 게이트: `dotnet build` 경고 0 / 전체 유닛 스위트 green(Shared 107 + Cli 620 + Mcp 25 + Core 169 = 921, 신규 eval 테스트 3 포함) / guardrail(카탈로그 stable-names + meta 커버리지) green / getting-started 문서 갱신.
  - CTL-FEEDBACK 2026-07-21 항목 Resolved 표기. 다음 게이트: C→D/E — 남은 흡수(P2 리졸버 통일, P3 친화 속성명, P4 플레이어 런타임) 및 러닝 F(재벤치마크 + 포지셔닝 v2).

## 참조 문서
- 공식 CLI: https://unity.com/blog/meet-the-unity-cli · https://docs.unity.com/en-us/unity-cli/unity-cli-reference · https://docs.unity3d.com/Packages/com.unity.pipeline@0.3/manual/index.html
- 공모전: `docs/contest/2026-oss-developer-contest.md`
- 격차 분석 근거: `src/Unityctl.Plugin/Editor/Commands/ExecHandler.cs` (리플렉션 표현식 평가기 — Roslyn 아님)
- 선례 브리프: `docs/goals/GOAL-spatial-grounding.md`
