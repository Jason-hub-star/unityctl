# AGENT.md — unityctl 작업 지침

이 문서는 `docs/` 전체와 루트 문서를 바탕으로 정리한, AI 에이전트/자동화/사람 협업자를 위한 실무 가이드다.  
목표는 프로젝트 소개가 아니라, 저장소 안에서 무엇이 사실이고 무엇이 아직 계획인지 빠르게 구분하고 안전하게 작업하게 돕는 것이다.

## 1. 프로젝트 한 줄 요약

`unityctl`은 AI 에이전트, CI, 사람이 Unity를 결정적으로 제어하기 위한 CLI다.  
장기 목표는 MCP 대체가 아니라 **MCP 상위 호환**이며, 현재 핵심 제공 가치는 다음 3가지다.

- CLI 기반이라 어떤 에이전트에서도 바로 호출 가능
- 구조화된 JSON 응답과 `StatusCode` 기반 복구 가능
- `unityctl tools --json`으로 도구를 동적으로 발견 가능

## 2. 현재 상태 요약

2026-03-17 기준:

- 완료: Phase 0, 0.5, 1A, 1B, 2A, 2A+
- 부분 완료: Phase 1C
- 완료: **Phase 2B (IPC Transport)**
- 다음 구현 우선순위: **Phase 2C (Async Commands)**
- 현재 transport: **IPC probe-first + Batch fallback**
- IPC는 실제 응답 경로로 동작 중이며, `ping/status/check/test-start` 검증 완료

현재 검증된 범위:

- `editor list`, `tools`, `tools --json`, `init`는 동작 확인됨
- `ping`, `status`, `check`는 열린 Editor 기준 실제 응답 확인됨
- `test`는 현재 `Busy` + `started asynchronously` 의미로 응답함
- `build`는 코드 구현은 있으나 열린 Editor 기준 실사용 검증은 아직 미완

주장해도 되는 것:

- compact structured output
- plugin bootstrap 자동화 (`init`)
- typed payload (`JsonObject`/`JObject`)
- `tools --json` 기반 tool discovery
- 열린 Editor에 대한 IPC 경로가 실제로 동작함

아직 주장하면 안 되는 것:

- 전체 워크플로우에서 토큰/시간 절감
- pure IPC latency가 항상 `<200ms`라고 단정하는 표현
- 열린 Editor `build`까지 전부 실사용 검증 완료됐다는 표현

## 3. 저장소 구조

```text
unityctl.slnx
├── src/Unityctl.Shared    # 프로토콜, 모델, 상수, JSON source-gen
├── src/Unityctl.Core      # transport, discovery, retry, executor
├── src/Unityctl.Cli       # 얇은 CLI 셸
├── src/Unityctl.Plugin    # Unity UPM 브릿지 (dotnet build 대상 아님)
├── tests/Unityctl.Shared.Tests
├── tests/Unityctl.Core.Tests
├── tests/Unityctl.Cli.Tests
└── tests/Unityctl.Integration.Tests
```

의존성 방향:

```text
Shared ← Core ← Cli
```

중요:

- Plugin은 Unity Editor 안에서만 컴파일된다
- Plugin은 `Shared`를 직접 참조하지 않고 일부 타입을 `Shared~` 복사본으로 유지한다
- Shared 프로토콜을 수정하면 Plugin 복사본 동기화 여부를 반드시 점검해야 한다

## 4. 에이전트가 먼저 알아야 할 실행 규칙

작업 전에 기본적으로 확인할 것:

```bash
dotnet build unityctl.slnx
dotnet test unityctl.slnx
```

빠른 테스트:

```bash
dotnet test unityctl.slnx --filter "FullyQualifiedName!~Integration"
```

CLI 예시:

```bash
dotnet run --project src/Unityctl.Cli -- editor list
dotnet run --project src/Unityctl.Cli -- tools --json
dotnet run --project src/Unityctl.Cli -- check --project "C:/MyGame" --json
```

현재 CLI 명령:

- `init`
- `editor list`
- `ping`
- `status`
- `build`
- `test`
- `check`
- `tools`

모든 명령은 `--json`을 지원한다.

## 5. 핵심 설계 불변식

### Payload와 직렬화

- CLI/Core는 `System.Text.Json`
- Plugin은 `Newtonsoft.Json`
- Payload 타입은 `Dictionary<string, object?>`가 아니라
  - CLI/Core: `JsonObject`
  - Plugin: `JObject`
- 파라미터 접근은 `GetParam()`, `GetParam<T>()`, `GetObjectParam()` 패턴을 유지한다

이 규칙을 깨면 serializer 간 payload 손실이 재발할 수 있다.

### Result 패턴

- 성공: `CommandResponse.Ok(data)`
- 실패: `CommandResponse.Fail(StatusCode, message)`
- 문자열 파싱보다 `StatusCode` 분기 우선

### StatusCode 의미

- `0`: Ready
- `100~103`: Transient, 재시도 가능
- `200`대: Fatal/환경 문제
- `500+`: 내부 오류

대표적인 해석:

- `NotFound`: Unity 미설치
- `ProjectLocked`: batch fallback 경로에서 프로젝트 잠금으로 실행 불가
- `PluginNotInstalled`: `init` 필요

### Transport 전략

- IPC 우선
- Batch 폴백

현재 현실:

- `CommandExecutor`는 probe-first로 IPC를 먼저 시도한다
- probe 실패 시에만 batch fallback 한다
- `SendAsync` 실패 후 batch 재실행은 하지 않는다

### 파이프명

- 파이프명은 `Constants.GetPipeName()`으로 생성
- SHA256 기반 결정적 이름 사용
- 프로젝트 경로는 `NormalizeProjectPath()` 규칙을 따라야 함

## 6. 코드 스타일과 구현 관례

- 클래스: PascalCase
- 인터페이스: `I` 접두사
- private 필드: `_camelCase`
- async 메서드: `Async` 접미사
- 기본적으로 `sealed class`
- 생성자 주입 선호
- 모든 async 경로에 `CancellationToken` 전달

빌드 속성:

- `LangVersion=12`
- `Nullable=enable`
- `TreatWarningsAsErrors=true`

즉, 경고도 실패다.

## 7. Plugin 작업 시 특별 주의

Plugin 관련 수정은 항상 별도로 생각해야 한다.

- `src/Unityctl.Plugin/`는 `dotnet build`로 검증되지 않는다
- Unity API 의존성 때문에 실제 검증은 Unity Editor 컴파일이 필요하다
- batchmode 응답은 stdout이 아니라 response-file 패턴이 핵심이다
- Unity 로그/exit code에만 의존하는 방식으로 회귀시키면 안 된다

Plugin에서 중요한 컴포넌트:

- `UnityctlBatchEntry`: batchmode 진입점
- `CommandRegistry`: handler 디스패치
- `IpcRequestRouter`: command 라우팅
- `IpcServer`: Phase 2B 완료, 현재 실구현 상태
- handlers: `Ping`, `Status`, `Build`, `Test`, `Check`

## 8. 테스트 해석법

현재 기준 테스트는 총 78개다.

- Shared.Tests: 19
- Core.Tests: 30
- Cli.Tests: 23
- Integration.Tests: 6

주의:

- Integration tests는 AppLocker 환경에서 skip 될 수 있다
- skip은 실패가 아니라 환경 회피로 설계되어 있다
- Plugin 관련 동작은 별도 Unity 수동 검증 없이는 완전 보장할 수 없다

## 9. 현재 가장 안전한 작업 우선순위

작업 방향을 모를 때는 아래 순서를 따른다.

1. Shared/Core/Cli 내부 변경인지 확인한다
2. Plugin 변경이 섞이면 Shared 복사본과 Unity 검증 필요성을 바로 표시한다
3. 현재 실제 구현인지, roadmap 상 계획인지 문서와 코드 둘 다 맞춘다
4. IPC 관련 작업이면 `docs/ref/phase-2b-plan.md`를 기준으로 움직인다
5. 장기 기능을 설명할 때는 반드시 `implemented`, `validated`, `planned`를 구분한다

## 10. 문서별 역할

- `docs/ref/getting-started.md`: 사용자/개발자 입문 흐름
- `docs/ref/ai-quickstart.md`: AI 에이전트용 실행 예시와 복구 흐름
- `docs/DEVELOPMENT.md`: 가장 정확한 진행 현황과 검증 범위
- `docs/ref/glossary.md`: 용어 기준점
- `docs/ref/phase-2b-plan.md`: IPC 구현 작업 기준 문서
- `docs/ref/phase-roadmap.md`: 전체 비전과 phase별 목표

우선 신뢰 순서:

1. 코드
2. `docs/DEVELOPMENT.md`
3. 개별 phase 문서
4. 시작 가이드 문서

## 11. 에이전트용 작업 가이드

### 문서/설명 작업

- 완료된 기능과 planned 기능을 섞어 쓰지 말 것
- "현재 가능"과 "로드맵"을 분리해서 적을 것
- 성능 수치나 토큰 절감은 문서에 이미 검증된 범위만 사용할 것

### 구현 작업

- transport/serialization 변경은 회귀 위험이 크므로 테스트부터 같이 만질 것
- Shared DTO 변경 시 Plugin 복사본/API parity를 같이 확인할 것
- CLI는 얇게 유지하고, 비즈니스 로직은 Core에 둘 것

### 리뷰 작업

- `Dictionary<string, object?>` 회귀
- `StatusCode` 우회한 문자열 기반 처리
- Plugin에서 stdout 의존 회귀
- Batch/IPC 경계 불명확
- Shared 변경 후 Plugin 복사본 미동기화

## 12. 다음 작업 대상으로 가장 중요한 것

현재 저장소에서 가장 중요한 미완 작업은 Phase 2C다.

핵심 체크포인트:

- `TestHandler` 결과 수집 모델
- batchmode와 IPC 공통 completion 의미
- Unity Test Framework callback 제약
- 도메인 리로드와 callback 재등록
- 결과를 `Busy`가 아닌 완료 응답으로 돌려주는 기준 정리

이 영역을 건드릴 때는 `docs/ref/phase-roadmap.md`와 `docs/status/PROJECT-STATUS.md`를 함께 본다.

## 13. 요약

이 저장소는 이미 "CLI + 구조화된 응답 + 기본 Unity automation + 열린 Editor IPC"까지는 올라와 있지만,  
`test`의 실제 완료 의미와 장기 실행 command 모델은 아직 Phase 2C 이후의 일이다.

에이전트는 다음 원칙만 지키면 된다.

- 현재 구현과 미래 계획을 절대 혼동하지 말 것
- Shared/Core/Cli와 Plugin의 경계를 존중할 것
- payload/serializer 규칙을 깨지 말 것
- Plugin 수정은 Unity 검증 없이는 완료라고 단정하지 말 것
