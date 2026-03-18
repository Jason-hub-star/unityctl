# Feature Backlog

추가 예정 기능 백로그. 지속적으로 업데이트합니다.

---

## Backlog Items

### 1. Addressable Asset System
- **난이도**: 중
- **예상 명령 수**: ~8
- **상태**: 대기
- **설명**: Unity Addressables 패키지 연동. 그룹/엔트리/라벨 CRUD 및 빌드 트리거.
- **예상 명령**:
  - `addressable group-list` — 그룹 목록 조회
  - `addressable group-create` — 그룹 생성
  - `addressable group-delete` — 그룹 삭제
  - `addressable entry-add` — 엔트리 추가
  - `addressable entry-remove` — 엔트리 제거
  - `addressable entry-move` — 엔트리 그룹 이동
  - `addressable label-set` — 라벨 설정
  - `addressable build` — Addressable 빌드 실행
- **주의사항**: Addressables 패키지 선택 설치 → `#if` 심볼 또는 리플렉션 분기 필요

### 2. Animator State Machine 제어
- **난이도**: 중
- **예상 명령 수**: ~6
- **상태**: 대기
- **설명**: AnimatorController의 레이어/스테이트/트랜지션/파라미터 CRUD.
- **예상 명령**:
  - `animator add-state` — 스테이트 추가
  - `animator remove-state` — 스테이트 제거
  - `animator add-transition` — 트랜지션 추가
  - `animator remove-transition` — 트랜지션 제거
  - `animator set-parameter` — 파라미터 추가/수정
  - `animator add-layer` — 레이어 추가
- **주의사항**: 기존 `animation-create-controller` 명령과 자연스럽게 연결

### 3. Animation 제어 및 Event 추가/삭제
- **난이도**: 중
- **예상 명령 수**: ~5
- **상태**: 대기
- **설명**: AnimationClip의 이벤트/커브/키프레임 CRUD.
- **예상 명령**:
  - `animation add-event` — 애니메이션 이벤트 추가
  - `animation remove-event` — 애니메이션 이벤트 제거
  - `animation list-events` — 이벤트 목록 조회
  - `animation add-curve` — 커브 추가
  - `animation set-curve` — 커브 키프레임 설정
- **주의사항**: `AnimationUtility.SetAnimationEvents()` / `GetAnimationEvents()` 사용

### 4. Timeline 제어
- **난이도**: 중~높
- **예상 명령 수**: ~6
- **상태**: 대기
- **설명**: TimelineAsset 트랙/클립 CRUD 및 PlayableDirector 바인딩.
- **예상 명령**:
  - `timeline create` — TimelineAsset 생성
  - `timeline add-track` — 트랙 추가 (Animation, Audio, Activation 등)
  - `timeline remove-track` — 트랙 제거
  - `timeline add-clip` — 클립 추가
  - `timeline remove-clip` — 클립 제거
  - `timeline bind` — PlayableDirector 바인딩 설정
- **주의사항**: Timeline 패키지 의존 → 조건부 컴파일 필요. 바인딩 설정 복잡도 높음.

### 5. Avatar 설정
- **난이도**: 낮~중
- **예상 명령 수**: ~3
- **상태**: 대기
- **설명**: Avatar/AvatarMask 설정 및 ModelImporter humanoid/generic 전환.
- **예상 명령**:
  - `avatar get-mask` — AvatarMask 조회
  - `avatar set-mask` — AvatarMask 설정
  - `avatar configure` — ModelImporter Avatar 타입 설정 (humanoid/generic)
- **주의사항**: API 비교적 단순. ModelImporter 재임포트 트리거 필요.

### 6. 프로파일링 분석 및 병목 검출
- **난이도**: 높
- **예상 명령 수**: ~5
- **상태**: 대기
- **설명**: Unity Profiler 데이터 캡처/분석. CPU/GPU/메모리 병목 상위 N개 추출.
- **예상 명령**:
  - `profiler start` — 프로파일링 시작
  - `profiler stop` — 프로파일링 중지
  - `profiler capture` — 프레임 데이터 캡처 (구간 지정)
  - `profiler analyze` — 캡처 데이터 분석 요약
  - `profiler hotspots` — 상위 N개 병목 함수/시스템 리스트
- **주의사항**:
  - PlayMode에서만 의미 있는 데이터 수집 가능 → PlayMode 연동 필수
  - 프로파일 데이터 크기 → IPC 10MB 제한 내 요약/필터링 전략 필요
  - `ProfilerDriver` + `Profiler` API 사용

---

## 요약

| # | 기능 | 난이도 | 명령 수 | 상태 |
|---|------|--------|---------|------|
| 1 | Addressable | 중 | ~8 | 대기 |
| 2 | Animator State Machine | 중 | ~6 | 대기 |
| 3 | Animation Event | 중 | ~5 | 대기 |
| 4 | Timeline | 중~높 | ~6 | 대기 |
| 5 | Avatar | 낮~중 | ~3 | 대기 |
| 6 | Profiling | 높 | ~5 | 대기 |
| | **합계** | | **~33** | |

## 공통 리스크
- **패키지 의존성**: Addressable, Timeline은 선택 패키지 → 조건부 컴파일 또는 리플렉션 필요
- **프로파일링 데이터 크기**: IPC 10MB 제한 내 요약/필터링 전략 필요
- **테스트**: Plugin 핸들러는 Unity Editor 실측만 가능 (dotnet test는 CLI 레이어만)

---

*최종 업데이트: 2026-03-18*
