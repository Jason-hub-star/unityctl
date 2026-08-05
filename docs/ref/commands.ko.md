# unityctl 명령어 레퍼런스

179개 CLI 진입점과, 정규화된 171개 명령 스키마를 필요할 때만 불러오는 12개 MCP 도구. 랜딩 페이지를 짧게 유지하려고 README에서 옮겼습니다 — 전체 표면은 이 문서가 정본입니다.

[English](commands.md) | [한국어](commands.ko.md)

---

## 명령어 (179)

### 코어 (14)

| 명령어 | 설명 |
|--------|------|
| `ping` | Unity 연결 확인 |
| `status` | 에디터 상태 확인 (Domain Reload 대응 `--wait` 폴링) |
| `check` | 스크립트 컴파일 검증 (헤드리스) |
| `build` | 플레이어 빌드 (`--dry-run`으로 13개 사전 검증) |
| `test` | EditMode / PlayMode 테스트 |
| `doctor` | 연결 진단 + 복구 조치 안내 |
| `project validate` | 게임 준비 상태 점검 (컴파일, 씬, 카메라, 조명, 콘솔, 에디터) |
| `init` | Unity 프로젝트에 플러그인 설치 |
| `mcp install` | Claude Code / Cursor / VS Code / Codex 설정에 MCP 서버 항목 기록 |
| `editor list` | 설치된 Unity 에디터 탐색 |
| `editor instances` | 실행 중인 Unity Editor 인스턴스 목록 |
| `editor current` | 현재 선택된 프로젝트 타겟 확인 |
| `editor select` | CLI 라우팅 대상 프로젝트 또는 PID 지정 |
| `workflow verify` | 아티팩트 우선 검증 (`projectValidate`, `capture`, `imageDiff`, `consoleWatch`, `uiAssert`, `playSmoke`) |

<details>
<summary><strong>씬 & 게임오브젝트</strong> (19)</summary>

| 명령어 | 설명 |
|--------|------|
| `scene snapshot` | 씬 상태 캡처 |
| `scene hierarchy` | 씬 계층 트리 |
| `scene diff` | 프로퍼티 수준 씬 비교 (epsilon 지원) |
| `scene save` | 활성 씬 저장 |
| `scene open` | 경로로 씬 열기 |
| `scene create` | 새 씬 생성 |
| `gameobject create` | 게임오브젝트 생성 |
| `gameobject delete` | 게임오브젝트 삭제 |
| `gameobject rename` | 게임오브젝트 이름 변경 |
| `gameobject move` | 게임오브젝트 부모 변경 |
| `gameobject find` | 이름/태그/컴포넌트로 검색 |
| `gameobject get` | 게임오브젝트 상세 정보 |
| `gameobject set-active` | 활성 상태 토글 |
| `gameobject set-tag` | 태그 설정 |
| `gameobject set-layer` | 레이어 설정 |
| `component add` | 컴포넌트 추가 |
| `component remove` | 컴포넌트 제거 |
| `component get` | 컴포넌트 프로퍼티 조회 |
| `component set-property` | 컴포넌트 프로퍼티 설정 |

</details>

<details>
<summary><strong>에셋 & 머티리얼</strong> (21)</summary>

| 명령어 | 설명 |
|--------|------|
| `asset find` | 타입/라벨/경로로 검색 |
| `asset get-info` | 에셋 메타데이터 |
| `asset get-dependencies` | 직접 의존성 |
| `asset reference-graph` | 역참조 그래프 |
| `asset create` | 에셋 생성 |
| `asset create-folder` | 폴더 생성 |
| `asset copy` | 에셋 복사 |
| `asset move` | 에셋 이동/이름 변경 |
| `asset delete` | 에셋 삭제 |
| `asset import` | 에셋 리임포트 |
| `asset refresh` | AssetDatabase 새로고침 |
| `asset get-labels` | 라벨 조회 |
| `asset set-labels` | 라벨 설정 |
| `material create` | 머티리얼 생성 |
| `material get` | 머티리얼 프로퍼티 조회 |
| `material set` | 머티리얼 프로퍼티 설정 |
| `material set-shader` | 셰이더 변경 |
| `prefab create` | 게임오브젝트에서 프리팹 생성 |
| `prefab unpack` | 프리팹 인스턴스 언팩 |
| `prefab apply` | 프리팹 오버라이드 적용 |
| `prefab edit` | 프리팹 편집 모드 진입/종료 |

</details>

<details>
<summary><strong>스크립팅 & 코드 분석</strong> (10)</summary>

| 명령어 | 설명 |
|--------|------|
| `script create` | 템플릿 기반 C# 스크립트 생성 |
| `script edit` | 스크립트 전체 교체 |
| `script patch` | 줄 단위 삽입/삭제/교체 |
| `script delete` | 스크립트 파일 삭제 |
| `script validate` | 컴파일 트리거 및 검증 |
| `script list` | MonoScript 에셋 목록 |
| `script get-errors` | 구조화된 컴파일 에러 (파일/줄/열/코드) |
| `script find-refs` | Unity를 시작하지 않고 로컬에서 심볼 참조 검색 |
| `script rename-symbol` | 전체 스크립트에서 심볼 이름 변경 (`--dry-run` 지원) |
| `exec` | Unity에서 C# 표현식 실행 |
| `exec eval` | 동봉 Roslyn 컴파일러로 다중 문장 C# 컴파일·실행, 도메인 리로드 없음 (opt-in: `AllowEval`) |
| `runtime status` / `runtime logs` | 실행 중인 Development Build 플레이어 조회 (씬·fps·캡처 로그) |

</details>

<details>
<summary><strong>에디터 제어</strong> (18)</summary>

| 명령어 | 설명 |
|--------|------|
| `play start/stop/pause` | 플레이 모드 시작/중지/일시정지 |
| `editor pause` | 에디터 일시정지 토글 |
| `editor focus-gameview` | Game View 포커스 |
| `editor focus-sceneview` | Scene View 포커스 |
| `player-settings get/set` | PlayerSettings 읽기/쓰기 |
| `project-settings get/set` | 프로젝트 설정 읽기/쓰기 |
| `console clear` | 콘솔 초기화 |
| `console get-count` | 로그/경고/에러 카운트 |
| `define-symbols get/set` | 스크립팅 정의 심볼 |
| `tag list/add` | 태그 관리 |
| `layer list/set` | 레이어 관리 |
| `undo` | 마지막 작업 취소 |
| `redo` | 마지막 취소를 되돌리기 |

</details>

<details>
<summary><strong>빌드 & 배포</strong> (6)</summary>

| 명령어 | 설명 |
|--------|------|
| `build-profile list/get-active/set-active` | 빌드 프로필 관리 |
| `build-target switch` | 빌드 플랫폼 전환 |
| `build-settings get-scenes/set-scenes` | 빌드 씬 목록 |

</details>

<details>
<summary><strong>물리, 조명 & NavMesh</strong> (12)</summary>

| 명령어 | 설명 |
|--------|------|
| `physics get-settings/set-settings` | DynamicsManager |
| `physics get-collision-matrix/set-collision-matrix` | 32x32 레이어 충돌 |
| `lighting bake/cancel/clear` | 라이트맵 베이킹 |
| `lighting get-settings/set-settings` | 라이트맵 설정 |
| `navmesh bake/clear/get-settings` | NavMesh |

</details>

<details>
<summary><strong>UI & 메시</strong> (8)</summary>

| 명령어 | 설명 |
|--------|------|
| `ui canvas-create` | UI Canvas 생성 |
| `ui element-create` | Button, Text, Image 등 생성 |
| `ui set-rect` | RectTransform 설정 |
| `ui find` | UI 요소 검색 |
| `ui get` | UI 요소 상세 정보 |
| `ui toggle` | Toggle 상태 설정 |
| `ui input` | InputField 텍스트 설정 |
| `mesh create-primitive` | Cube/Sphere/Plane/Cylinder/Capsule/Quad 생성 |

</details>

<details>
<summary><strong>자동화 & 모니터링</strong> (15)</summary>

| 명령어 | 설명 |
|--------|------|
| `batch execute` | 롤백 포함 트랜잭션 실행 |
| `workflow run` | JSON 워크플로우 실행 |
| `watch` | 실시간 이벤트 스트리밍 |
| `log` | 플라이트 레코더 조회 |
| `session list/stop/clean` | 세션 관리 |
| `screenshot` | Scene/Game View 캡처 (base64) |
| `schema` / `tools` | 머신 리더블 메타데이터 |
| `package list/add/remove` | 패키지 관리 |
| `animation create-clip/create-controller` | 애니메이션 에셋 |

</details>

---

## 선택 기반 라우팅

```bash
# 사용할 Unity 프로젝트를 한 번 지정해두면
unityctl editor select --project /path/to/project

# 또는 실행 중인 Unity PID로 지정
unityctl editor select --pid 55028

# 현재 어디를 가리키고 있는지 확인
unityctl editor current --json

# 실행 중인 Unity 인스턴스 목록 (PID / 프로젝트 / IPC 상태)
unityctl editor instances --json

# 이후부터는 --project 생략 가능
unityctl ping --json
unityctl status --json
unityctl check --json
unityctl doctor --json

# 검증 번들 실행 (아티팩트 우선)
unityctl workflow verify --file verify.json --project /path/to/project --json
```


## 12개 MCP 도구

| 도구 | 타입 | 설명 |
|------|------|------|
| `unityctl_query` | 읽기 | 에셋, 게임오브젝트, 씬, 컴포넌트, UI, 물리, 조명, 태그 통합 조회 |
| `unityctl_run` | 쓰기 | 생성, 삭제, 수정, 스크립트, 머티리얼, 프리팹, 배치 통합 실행 |
| `unityctl_schema` | 메타 | 명령별 또는 카테고리별 파라미터 온디맨드 조회 |
| `unityctl_build` | 액션 | 13개 사전 검증 포함 플레이어 빌드 |
| `unityctl_check` | 액션 | 컴파일 검증 (헤드리스 지원) |
| `unityctl_test` | 액션 | EditMode / PlayMode 테스트 |
| `unityctl_exec` | 액션 | 임의의 C# 표현식 실행 |
| `unityctl_status` | 읽기 | 에디터 상태 + 연결 정보 |
| `unityctl_ping` | 읽기 | 연결 확인 |
| `unityctl_watch` | 스트림 | 실시간 콘솔/계층/컴파일 이벤트 |
| `unityctl_log` | 읽기 | 플라이트 레코더 조회 |
| `unityctl_session_list` | 읽기 | 활성 세션 목록 |
