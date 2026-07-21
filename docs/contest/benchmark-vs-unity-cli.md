# unityctl vs 공식 Unity CLI 벤치마크

> GOAL-unity-cli-benchmark 러닝 A 산출물 (2026-07-21). 동일 프로젝트·동일 에디터 세션에서 8태스크를 양쪽 도구로 실행해 성공 여부·레이턴시·응답 크기를 측정했다. 공식 도구의 실패도, 우리 도구의 실패도 그대로 기록한다.

## 환경

| 항목 | 값 |
|---|---|
| 날짜 | 2026-07-21 |
| OS | macOS (Darwin 25.3.0, arm64), 측정 대부분 **화면 잠금(무인) 상태** |
| Unity Editor | 6000.0.64f1 (헤디드, 비포커스 백그라운드) |
| 공식 Unity CLI | 1.0.0-beta.2 + com.unity.pipeline 0.3.1-exp.1 (localhost:7800 HTTP) |
| unityctl | 0.5.0 @ be84722 — Release 빌드, 경고 0 (Named Pipe IPC) |
| 프로젝트 | SampleUnityProject 사본 (스크래치, 양쪽 브릿지 동시 설치) |
| 측정 | wall-clock ms + stdout+stderr bytes, 도구별 native 권장 호출 (`measure.sh`) |

무인(unattended) 조건 주의: 측정 중 디스플레이가 잠겨 있었고 에디터는 한 번도 전면에 오지 않았다. 이 조건이 T6/T8에서 양쪽 모두의 핵심 약점을 드러냈다 — 에이전트가 CI·원격·야간에 에디터를 부리는 시나리오와 정확히 같은 조건이므로 별도 섹션으로 기록한다.

## 결과 요약

- **읽기/쓰기 왕복은 unityctl이 일관되게 빠르고 작다** (T1 hierarchy 2.2배 빠름·43% 작음, T4 play 사이클 2.7배 빠름). Named Pipe + summary-by-default가 HTTP + 전체 페이로드보다 에이전트 왕복에 유리.
- **라이브 C# 실행은 공식의 압승** (T7b: Roslyn eval로 루프·집계 실행 성공, 우리 exec는 설계상 단일 표현식만 — 파서 거부).
- **무인 에디터 생존성은 갈렸다**: 도메인 리로드 후 공식 서버는 739ms에 복귀(static-ctor 재기동 + 자체 메인스레드 디스패처), **unityctl 브릿지는 재기동 실패**(delayCall/update 큐 기아 → 141초 타임아웃, CTL-FEEDBACK 등재). 반대로 **테스트 실행은 무인 cold 조건에서 공식이 0개 테스트 가짜 성공**, unityctl은 정상 실행(1 passed).
- 공식 CLI의 에이전트 적대적 UX 다수 확인: 잘못된 인자 **조용히 무시 후 성공 반환**(가짜 성공), eval은 `return` 문장 강제, test_status는 이중 인코딩 JSON 문자열 반환.

## 태스크별 결과

표기: 레이턴시ms / 응답bytes / 판정. 다단계 태스크는 하위 호출 합산. 원자료는 부록 참조.

| # | 태스크 | unityctl | 공식 CLI |
|---|---|---|---|
| T1 | scene hierarchy 읽기 | 286ms / 919B / 성공 (summary-by-default) | 617ms / 1602B / 성공 |
| T2 | 프리미티브 생성 + Rigidbody + mass=5 (3콜) | 848ms / 2821B / 성공 | 2456ms / 3041B / 성공 |
| T3 | 프리팹 생성 + 인스턴스화 (2콜) | 805ms / 1756B / 성공 (단, `--target`은 globalObjectId만 수용) | 777ms / 1624B / 성공 (이름 해석 수용) |
| T4 | play 진입 + 콘솔 수집 + 정지 (3콜) | 965ms / 6343B / 성공 (콘솔 dedupe 5150B) | 2588ms / 7792B / 성공 (콘솔 6867B) |
| T5 | 스크린샷 캡처 | 853ms / 239KB / 성공 (카메라 0개여도 뷰 캡처) | 336ms / 286B / **실패** — "No camera found" (씬에 카메라 없음) |
| T6 | EditMode 테스트 (1개) | cold 13234ms 성공 · warm 4215ms 성공 | warm(유인 상태) 1959ms 성공 · **무인 cold 가짜 성공** (Total 0, "no_tests") |
| T7 | 라이브 C# — (a) 단일 표현식 (b) 다중 문장 | (a) 231ms 성공 (b) **실패** — 파서 거부(설계 한계) | (a) 1108ms 성공(`return` 문장 강제) (b) 2634ms 성공 — MeshRenderer 집계 result=4 |
| T8 | 도메인 리로드 후 재연결 (무인 에디터) | **실패** — 브릿지 재기동 안 됨, 90초 예산 소진 + batch 폴백 lock 충돌로 141656ms | 739ms / 성공 — static-ctor 재기동으로 생존 |

## 무인 에디터 신뢰성 (핵심 발견)

에이전트 운용의 실전 조건(포커스 없음·화면 잠금·야간 CI)에서 양쪽 다 균열이 드러났고, 균열의 위치가 다르다.

**unityctl의 균열 — 브릿지 수명주기가 update 큐에 의존**
`UnityctlBootstrap`이 `EditorApplication.delayCall`+`update` 게이트로 시작을 지연하는데, 한 번도 포커스되지 않은 에디터에서는 이 큐가 아예 흐르지 않는다. 신규 부팅에서 브릿지가 시작조차 못 했고(editor4.log에 startup 로그 부재), 도메인 리로드 후 재기동도 실패해 `ipc-state.json`이 `reloading`에 고착됐다. 공식 pipeline은 `[InitializeOnLoad]` static ctor에서 즉시 서버를 올리고 명령 실행도 자체 메인스레드 디스패처를 써서 같은 조건에서 생존했다. 워크어라운드 2종 검증: 에디터 포커스 부여, 또는 공식 `eval`로 `UnityctlBootstrap.StartBridge`를 리플렉션 호출. **수정 방향(러닝 C 후보 0순위): static-ctor 직접 기동 + `AssemblyReloadEvents.afterAssemblyReload` 재기동 + update 큐 비의존 메인스레드 디스패치.** (CTL-FEEDBACK 2026-07-21, severity high)

**공식의 균열 — 조용한 퇴화(가짜 성공)**
같은 무인 조건에서 공식 `run_tests`는 테스트 0개를 찾고도 success:true(Total 0)를 반환했다(직후 unityctl `test`는 같은 에디터에서 4.2초에 1 passed — Test Runner 구동 방식 차이). 인자 문법이 틀려도(`name=X` key=value 형식) 에러 없이 기본값으로 실행되고 성공을 반환한다. 리로드 창에서는 간헐적 "Cannot connect"도 관측됐다. 에이전트 관점에서 가짜 성공은 명시적 실패보다 위험하다 — 검증 루프가 오염된다.

## 세부 관찰

- **속성명**: 양쪽 모두 friendly name(`mass`) 거부, serialized name(`m_Mass`) 요구. 친화 이름 매핑은 양쪽 다 없는 차별화 기회.
- **리졸버 일관성**: 공식은 objectref를 이름/hierarchyPath/globalId 어디로든 받음. unityctl은 명령마다 다름(`component add`는 이름 OK, `prefab create --target`은 globalObjectId만) — 통일 필요.
- **응답 규율**: unityctl 콘솔 dedupe(5150B vs 6867B), hierarchy summary(919B vs 1602B) 등 토큰 규율이 수치로 확인됨. 공식 `test_status`는 result가 JSON-in-JSON 문자열이라 파싱 2회 필요.
- **공식 eval 진입장벽**: bare 표현식은 CS1002 에러 — `return expr;` 문장 형태를 알아야 함. 우리 exec는 표현식 즉시 평가(231ms)로 빠른 인스펙션에 유리하나 상한이 낮다.
- **T5 철학 차이**: 공식 `capture_game_view`는 씬에 카메라가 없으면 실패(정직하지만 막힘), unityctl은 뷰를 그대로 캡처(에이전트 관대). 어느 쪽이 옳은지는 용례 따라 다르나, 실패 메시지에 "카메라를 추가하라"는 안내가 없는 것은 공통 감점.

## 흡수 우선순위 제안 (승인 게이트 입력)

측정이 바꾼 순위: 사전 예상은 Roslyn eval이 1순위였으나, T8이 존재론적 결함(무인 운용 불가)을 드러내 수명주기 수정이 0순위로 승격.

1. **P0 — 브릿지 수명주기 재설계** (static-ctor 기동, afterAssemblyReload 재기동, update 큐 비의존 디스패치). 무인 에이전트 운용의 전제조건.
2. **P1 — Roslyn eval** (opt-in + BlockedPatterns 레이어 유지). T7b에서 확인된 유일한 능력 격차.
3. **P2 — ObjectRef 리졸버 통일** (모든 write 명령이 이름/경로/globalId 수용).
4. **P3 — 친화 속성명 매핑** (`mass`→`m_Mass` 자동 해석 + 실패 시 후보 제시). 양쪽 다 없어 차별화 기회.
5. **P4 — 플레이어 런타임 제어** (이번 측정 범위 밖, 공식 고유 능력으로 확인됨).

## v2 재측정 — 흡수 후 (2026-07-21, 같은 날)

v1이 정한 흡수 우선순위 P0~P4를 같은 날 전부 구현하고, 변경된 셀을 동일 환경(무인 에디터)에서 재측정했다. **9/9 성공.**

| 항목 | v1 (흡수 전) | v2 (흡수 후) |
|---|---|---|
| T8 리로드 생존 (무인) | 실패 — 141,656ms 타임아웃 | **성공** — 리로드 창 516ms / 직후 313ms, 브릿지 무개입 재기동 |
| 무인 부팅 브릿지 기동 | 기동 안 됨 (포커스 필요) | **자동 기동** (재측정 세션 자체가 무킥으로 진행) |
| T7b 다중 문장 C# | 파서 거부 | **성공** — `exec eval` 1,755ms (공식 2,634ms보다 33% 빠름) |
| T2c 속성 설정 | `mass` 실패 → `m_Mass` 재시도 (2콜) | **`mass` 첫 시도 성공** 358ms (1콜) |
| T3a 프리팹 생성 | 이름 거부 → find+globalId 우회 (2콜) | **이름 타깃 1콜 성공** 415ms |
| 플레이어 런타임 | 없음 (공식 고유) | **`runtime status`/`runtime logs`** — 실행 중 dev 플레이어 라이브 검증 |
| T1 hierarchy (회귀 체크) | 286ms | 339ms — 회귀 없음 (씬 오브젝트 증가분) |

v1에서 공식이 이겼던 셀(T7b, T8)은 모두 역전됐고, 공식 고유였던 플레이어 런타임은 v1급 커버리지(status/logs)로 흡수됐다. 남은 공식 우위: runtime hot-reload/input-sim (백로그).

## 부록: raw transcript

- 측정 스크립트·결과: 스크래치 `bench/measure.sh`, `bench/results.ndjson`, `bench/raw/` (도구-태스크별 전체 응답)
- 도구별 마지막 기록 집계: `bench/aggregate.py results.ndjson`
- 발췌 — T8 unityctl 실패: `"IPC message timed out (30s)"` → 90s 예산 소진 → `"Project lock is still held"` (141656ms, exit 1) / T8 공식 성공: `"status": "ready"` (739ms)
- 발췌 — T6 무인 cold 공식: `run_tests` → `"Summary": {"Total": 0, ...}` + `test_status` → `"no_tests"` / 직후 unityctl: `"Tests completed: 1 passed (3.7s)"`
- 발췌 — T7b 공식 성공: `"result": 4, "executionTimeMs": 1877` / unityctl 거부: `"Parse error at char 1: expected 'TypeName.MemberName'"`
