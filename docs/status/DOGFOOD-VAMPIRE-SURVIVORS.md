# Dogfood Loop — Vampire Survivors (build a full game to self-improve unityctl)

**Directive (user, 2026-07-16):** unityctl로 완전한 뱀서(Vampire Survivors)류 게임을 만들면서 ctl의 취약점·개선점을 파악하고 **자기개선(ctl 수정·커밋)**한다. 무한루프 — 사용자가 "끝"이라 할 때까지 계속 개선점을 찾아 ctl을 self-improve. 시중 뱀서 장점을 모아 완전한 게임 목표.

**진짜 deliverable = unityctl self-improvement.** 게임은 ctl 갭을 드러내는 vehicle. 게임 진척 + 발견한 ctl 갭 + 적용한 ctl 수정을 매 iteration 기록.

**게임 vehicle 프로젝트:** `/Users/family/jason/unityctl-demo` (Unity 6000.3.16f1, 플러그인 설치·IPC ready). 게임 코드: `Assets/VampireSurvivors/`. ctl 수정: 이 repo(`/Users/family/jason/unityctl`).

## 루프 프로토콜 (매 iteration)
1. 다음 게임 기능 1개를 unityctl 명령만으로 구현 시도.
2. 막히거나 어색하거나 버그면 → **ctl 갭**으로 기록 → 그 자리에서 ctl 수정(코드+테스트+커밋).
3. play mode / spatial / screenshot / console로 직접 검증.
4. 이 문서의 진척·갭·수정 로그 갱신. 다음 iteration으로.
5. 컨텍스트가 요약돼도 이 문서만 읽으면 이어갈 수 있게 유지(SSOT).

## 게임 로드맵 (시중 뱀서 장점 종합)
- [x] M1 Player: top-down 이동(WASD) — **검증완료**(play mode에서 x 0.32→71.68→87.15, moveSpeed=8). 카메라 **static top-down 배치완료**(pos [0,18,0], rot look-down). follow 스크립트는 미완
- [ ] M2 Enemy: 스폰 + 플레이어 추격(chase), 시간에 따른 난이도 스케일
- [ ] M3 Combat: 자동공격 무기(투사체/오라) — VS 시그니처(무기 자동)
- [ ] M4 XP/Level: 젬 드롭·픽업 → 레벨업 3카드 선택
- [ ] M5 Weapons: 다무기(채찍/마법봉/마늘오라/단검) + 진화(evolution)
- [ ] M6 Passives + 스탯(Brotato식 stat 빌드)
- [ ] M7 Waves/Boss: 시간축 난이도·엘리트·보스
- [ ] M8 Meta: 생존타이머·HP·게임오버·골드·영구 업그레이드(Halls/Brotato식)
- [ ] M9 Juice: 사운드·파티클·히트 피드백·화면 흔들림
- [ ] M10 Balance pass + 완성도

장점 출처: Vampire Survivors(무기 자동·진화), 20 Minutes Till Dawn(조준·리로드 변주), Brotato(상점·스탯 빌드), Halls of Torment(루팅·스탯 화면).

## ctl 갭 로그 (발견 → 조치)
| # | iteration | 갭/마찰 | 심각도 | 조치(커밋) |
|---|---|---|---|---|
| 1 | it1 | `scene create`가 부모 폴더 없으면 실패 — mkdir -p 미지원 | P2 | **FIXED+검증**: SceneCreateHandler `EnsureAssetFolder`. Unity 6000.3.16f1에서 `Assets/VampireSurvivors/Game.unity` 폴더 자동생성 확인 |
| 2 | it1 | `script create`가 초기 content 미지원 → create→edit 2단계, 각각 도메인 리로드 유발. 게임빌드는 스크립트 다수 생성 → 리로드 폭주로 후속 명령 블록/타임아웃 | P2(중요) | **FIXED+검증**: `script create --content/--content-file` 추가(7계층+테스트). 라이브: ContentProbe.cs를 1명령에 내 내용으로 생성 확인(GAP2_MARKER) |
| 5 | it2 | **부하 시 도메인 리로드가 "reloading"에 5~10분 멈춤** — 부트스트랩이 `isUpdating`(패키지/ILPP) 완료를 무한정 대기, 타임아웃/폴백 없음. Unity 포커스(`open -a`) nudge로 복구됨. 재컴파일 많은 세션에서 재현적(주: 이 세션이 Plugin을 반복 재컴파일해 머신 포화시킨 게 증폭 원인 — 정상 에이전트 사용에선 덜함) | P2(로봇성) | **관찰·재현**: 매번 focus nudge로 복구. 근본 fix 후보: 부트스트랩 재시작 워치독/타임아웃, 리로드 없는 배치 스크립트 쓰기 |
| 6 | it2 | **직접 transform setter 부재**: 생성 후 오브젝트 위치/회전을 바꾸려면 Transform 컴포넌트의 `m_LocalPosition`/`m_LocalRotation` SerializedProperty를 set-property로 건드려야 하고, 회전은 **raw 쿼터니언**을 줘야 함(에이전트에 불친절). `gameobject move`는 재부모화지 위치이동 아님 | P2(ergonomics) | **후보**: `gameobject set-transform --position/--rotation(euler)/--scale` 신규 명령. gap#3 배열형 재사용 |

**gap#3 추가검증**: 카메라 배치에서 Vector3 `[0,18,0]` + Quaternion `[0.7071,0,0,0.7071]` 배열형 모두 성공 — gap#3 fix가 Vector2뿐 아니라 Vector3·Quaternion에도 동작함 확인.

| 4 | it2 | play mode 시간검증 스로틀 (재확인) | P1 | **메커니즘 검증완료, 구현 BLOCKED**: `play pause` 후 `EditorApplication.Step`을 12회(각 별도 IPC호출) → Player x 0.32→2.24, 포커스 무관 결정적 전진 확인. 단 동기 루프는 coalesce 가능 → N프레임엔 async 필요. + CLI verb 추가가 gap#7로 막힘 → 이번 턴 구현 롤백, 다음 재개로 이월(async + gap#7 우회) |
| 7 | it3 | **ConsoleAppFramework 5.3.3 CLI verb 추가 불가**: `app.Add`로 새 verb 추가 시 `--help`엔 뜨는데 **dispatch case가 생성 안 됨**("Unknown command") — clean rebuild(obj/bin 삭제)로도 재현. `play advance`/`play-step`/`sim step` 모두 list-but-no-dispatch, 반면 `spatial describe`·`play start/stop/pause`는 정상. 신규 CLI verb 추가를 봉쇄 | **P1(개발 봉쇄)** | **조사 필요**: CAF 증분 제너레이터 버그로 추정. 우회안 — 기존 명령에 action 추가(예: play-mode action=step), CAF 버전 업/교체, 또는 별도 진입점. 신규 명령은 catalog/MCP엔 넣되 CLI verb는 이 이슈 해결 후 |
| (관찰) | it1 | 스크립트 생성 직후 명령은 도메인 리로드 대기로 블록됨(리로드 내성 fix 덕에 타임아웃 대신 대기하나 느림). 에이전트는 리로드 사이 settle을 기다려야 함 | — | 관찰. gap#2 fix가 리로드 횟수 절반으로 완화 |
| 3 | it2 | `component set-property`가 Vector2/3/4·Color를 `{"x":..}` 객체형만 받고 `[x,y,z]` 배열형은 거부 — 근데 `mesh create-primitive`는 `[x,y,z]`를 씀 → **포맷 불일치 트랩**. 에이전트가 position 포맷 재사용하면 실패 | P1(핵심, 게임빌드 상시) | **FIXED+검증**: `TryReadFloatArray` 추가(Vector2/3/4·Quaternion·Color 배열형 수용). 라이브: `manualDirection=[1,0]` → `{"x":1,"y":0}` 성공 |
| 4 | it2 | **에디터 비포커스 시 play mode 프레임 업데이트가 스로틀**됨 → 에이전트가 시간기반 게임플레이(이동/스폰/타이머)를 검증할 때 Update가 거의 안 돌아 위치 변화가 안 잡힘. 포커스하면 정상(x 0.32→71→87). 즉 에이전트의 자율 검증루프가 play mode 관찰에서 불안정 | P1(자율 verify loop 핵심) | **계획**: `play step --frames N`(또는 `--seconds S`) 신규 명령 — `EditorApplication.Step()`로 포커스 무관 결정적 프레임 전진 후 상태 읽기. 다음 iteration 최우선 |

## ctl 자기개선 커밋 로그
- `53368cb` IPC reload staleness fix + asset-copy 경로 가드
- `8281fe2` gap#1: scene create mkdir -p
- `b32997b` gap#3: set-property 벡터/컬러 배열형 수용
- `7c90224` M1 검증 체크포인트
- (gap#2 `script create --content-file` — 이 커밋)
- (그 외 이 세션: spatial grounding `16e1f33`/`7f0d2ab`, fleet cap `ee26798`)

## 현재 상태 / 다음 액션 (RESUME POINT)
- **게임 진척(M1 done)**: 씬 `Assets/VampireSurvivors/Game.unity` + `Scripts/PlayerMovement.cs`(agent-testable 이동) + Player 캡슐 + PlayerMovement 컴포넌트. **play mode에서 이동 검증완료**(포커스 시 x 0.32→71→87). Player GOID `...308252`, PlayerMovement 컴포넌트 GOID `...308257`.
- **에디터**: pid 53435, IPC ready. (비포커스 스로틀 주의 — gap#4)
- **다음 액션 (재개 시, 우선순위)**:
  1. **gap#4 최우선**: `play step --frames N`/`--seconds S` 신규 명령(EditorApplication.Step, 포커스 무관 결정적 전진). 자율 verify loop의 핵심 인프라. 7계층 + 테스트 + e2e.
  2. gap#2: `script create --content-file`(create→edit 2리로드를 1로).
  3. 카메라 top-down follow.
  4. M2 적: 스폰 + 플레이어 추격 스크립트 → play step으로 검증.
- **환경 메모**: 게임=`/Users/family/jason/unityctl-demo`(Unity 6000.3.16f1). ctl 수정=이 repo. **Plugin 수정 절차**: 소스 편집 → 데모 plugin에 `cp` sync → `open -a`로 Unity 포커스(recompile) → ipc-state fresh "ready" 대기 → 테스트. 빌드 dll: `src/Unityctl.Cli/bin/Debug/net10.0/unityctl.dll`. **주의**: 머신 부하 시 리로드가 길어짐(포커스로 nudge). play mode 시간검증은 포커스 필요(gap#4 fix 전까지).
