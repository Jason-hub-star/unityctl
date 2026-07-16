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
- [ ] M1 Player: top-down 이동(WASD), 카메라 follow
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
| 2 | it1 | `script create`가 초기 content 미지원 → create→edit 2단계, 각각 도메인 리로드 유발. 게임빌드는 스크립트 다수 생성 → 리로드 폭주로 후속 명령 블록/타임아웃 | P2(중요) | 계획: `script create --content/--content-file` 추가(1명령=1리로드). 다음 iteration |
| (관찰) | it1 | 스크립트 생성 직후 명령은 도메인 리로드 대기로 블록됨(리로드 내성 fix 덕에 타임아웃 대신 대기하나 느림). 에이전트는 리로드 사이 settle을 기다려야 함 | — | 관찰. gap#2 fix가 리로드 횟수 절반으로 완화 |
| 3 | it2 | `component set-property`가 Vector2/3/4·Color를 `{"x":..}` 객체형만 받고 `[x,y,z]` 배열형은 거부 — 근데 `mesh create-primitive`는 `[x,y,z]`를 씀 → **포맷 불일치 트랩**. 에이전트가 position 포맷 재사용하면 실패 | P1(핵심, 게임빌드 상시) | **FIXED+검증**: `TryReadFloatArray` 추가(Vector2/3/4·Quaternion·Color 배열형 수용). 라이브: `manualDirection=[1,0]` → `{"x":1,"y":0}` 성공 |

## ctl 자기개선 커밋 로그
- `SceneCreateHandler` mkdir -p (gap#1) — 커밋 예정 이번 checkpoint

## 현재 상태 / 다음 액션 (RESUME POINT)
- **게임 진척**: 씬 `Assets/VampireSurvivors/Game.unity` 생성됨. `Scripts/PlayerMovement.cs` **템플릿만** 생성(내용 미기입 — script edit가 리로드 대기로 미완). Player 오브젝트 아직 없음.
- **에디터**: pid 53435, 방금 script create로 reloading 상태 → settle 대기 필요.
- **다음 액션 (재개 시)**:
  1. IPC ready 대기 후 `script edit --content-file`로 PlayerMovement 내용 기입(scratchpad/PlayerMovement.cs = agent-testable 이동: manualInput/manualDirection).
  2. gap#2 구현: `script create`에 `--content-file` 추가 → sync/recompile/test/commit.
  3. Player 캡슐 생성 + PlayerMovement 컴포넌트 add + top-down 카메라.
  4. play mode 검증: manualInput=true, manualDirection=(1,0), play start, position.x 증가 확인, play stop.
- **환경 메모**: 게임=`/Users/family/jason/unityctl-demo`(Unity 6000.3.16f1, IPC ready). ctl 수정=이 repo. Plugin 수정 후 반드시 데모 plugin에 sync + Unity focus로 recompile + IPC ready 확인 후 테스트. 빌드 dll: `src/Unityctl.Cli/bin/Debug/net10.0/unityctl.dll`.
