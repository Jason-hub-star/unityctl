# unityctl — 현재 지원 기능 (코드 기준 검증)

---

## 1. 에디터 제어

- 핑 / 상태 확인
- 플레이 시작 / 일시정지 / 정지
- 컴파일 상태 확인

## 2. 씬 & 오브젝트 제어

- 게임오브젝트 생성 / 삭제 / 이동 / 이름변경 / 활성화·비활성화
- 컴포넌트 추가 / 제거 / 속성 변경 (SerializedProperty)
- 게임오브젝트 검색 / 단건 조회 (`gameobject find`, `gameobject get`)
- 컴포넌트 단건 조회 / 단일 property 조회 (`component get`)
- lightweight 씬 계층 조회 (`scene hierarchy`)
- 씬 열기 / 저장 / 생성 (single / additive 모드)

## 3. 씬 Diff

- 씬 스냅샷 저장
- 이전 상태와 비교 (--live 모드 지원)
- 어떤 오브젝트의 어떤 속성이 어떻게 바뀌었는지 추적 가능

## 4. 에셋 & 프리팹 관리

- 에셋 생성 / 복사 / 이동 / 삭제 / 강제 리임포트
- 에셋 검색 / 단건 메타 조회 / 의존성 조회 (`asset find`, `asset get-info`, `asset get-dependencies`)
- 에셋 역참조 그래프 조회 (`asset reference-graph`)
- Build Settings 씬 목록 조회 (`build-settings get-scenes`)
- 폴더 생성 / AssetDatabase 새로고침
- 프리팹 생성 / 적용(Apply) / 언팩(Unpack) / 내부 편집(Edit)

## 5. 머티리얼 · 애니메이션 · UI

- 머티리얼 속성 조회 / 변경 / 셰이더 변경
- 애니메이션 클립 생성 / 컨트롤러 생성
- Canvas 생성 (3가지 렌더 모드)
- UI 요소 9종 생성 (Button, Text, Image, Panel, InputField, Toggle, Slider, Dropdown, ScrollView)
- RectTransform 속성 설정

## 6. 빌드 & 테스트

- 6개 플랫폼 빌드 (Windows, macOS, Linux, Android, iOS, WebGL)
- 고스트 모드 (--dryRun) → 빌드 전 사전 검증
- EditMode / PlayMode 테스트 실행
- 테스트 필터 / 타임아웃 지원

## 7. 패키지 · 프로젝트 설정 · Undo · C# 실행

- 패키지 목록 / 추가 / 제거
- 프로젝트 설정 조회 / 변경 (editor, graphics, quality, physics, time, audio)
- PlayerSettings 조회 / 변경
- 모든 작업 Undo / Redo 가능
- 임의 C# 코드 실행 (코드 문자열 또는 .cs 파일)
- 워크플로우 실행 (JSON으로 명령 시퀀스 정의)

## 8. 실시간 모니터링

- 콘솔 로그 스트리밍 (console, hierarchy, compilation 채널)
- 명령 이력 자동 기록 (Flight Recorder, NDJSON)
- 로그 조회 / 필터 / 보존 정책 / 통계
- 세션 관리 (목록 / 중지 / 정리)
- `doctor` 진단 명령 + 연결/리로드 계열 실패 시 자동 doctor 요약

## 9. AI 연동 (MCP)

- MCP 서버 내장
- AI가 Unity를 직접 조작 가능
- 23개 MCP 도구
- 44개 write 명령 allowlist
- 10개 read query MCP 도구 (`asset/gameobject/component/scene/build-settings/reference/screenshot`)

## 10. 인프라

- IPC Named Pipe 통신 (~100ms)
- 배치 폴백 → CI 환경에서도 사용 가능
- Windows / macOS / Linux 지원
- .NET 10 + Unity 2021.3+ 호환

---

**총 74개 CLI 명령 · 23개 MCP 도구 · 448개 테스트**
