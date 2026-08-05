# unityctl README 부록

실사용 예제, 환경 지원, 아키텍처 다이어그램 — 랜딩 페이지를 짧게 유지하려고 README에서 옮겼습니다. 삭제한 내용은 없고 위치만 이동했습니다.

[English](readme-appendix.md) | [한국어](readme-appendix.ko.md)

---

## AI 에이전트로 만들 수 있는 것

### 씬 구성

> _"바닥, 벽, 플레이어 스폰 포인트가 있는 플랫포머 레벨 만들어줘"_

```bash
# 씬 구조 생성
unityctl scene create --path "Assets/Scenes/Level01.unity" --project $P
unityctl mesh create-primitive --type Plane --name "Floor" --scale "[10,1,10]" --project $P
unityctl mesh create-primitive --type Cube --name "Wall" --position "[5,1,0]" --scale "[0.5,2,10]" --project $P
unityctl gameobject create --name "PlayerSpawn" --project $P
unityctl component add --id "<PlayerSpawnId>" --type "BoxCollider" --project $P

# 씬 검증
unityctl scene hierarchy --project $P --json      # 구조 확인
unityctl screenshot capture --project $P           # 시각 검증
unityctl project validate --project $P --json      # 카메라? 조명? 에러?
```

### 스크립트 작성 + 컴파일 검증

> _"플레이어 이동 스크립트 작성하고 컴파일 되는지 확인해줘"_

```bash
# 코드 작성
unityctl script create --path "Assets/Scripts/PlayerMovement.cs" --className "PlayerMovement" --project $P
unityctl script patch --path "Assets/Scripts/PlayerMovement.cs" \
  --startLine 8 --insertContent "public float speed = 5f;" --project $P

# 컴파일 확인 → 에러 발생 시 루프로 수정
unityctl script validate --project $P --wait       # 리컴파일 트리거
unityctl script get-errors --project $P --json     # 구조화된 CS 에러
# 에러가 있으면: 에러 확인 → 패치 수정 → 재검증
```

### 안전한 배치 작업 (롤백 포함)

> _"Player, Enemy, Projectile 물리 레이어 설정해줘 — 실패하면 되돌려"_

```bash
unityctl batch execute --project $P --rollbackOnFailure true --commands '[
  {"command": "layer-set", "parameters": {"index": 8, "name": "Player"}},
  {"command": "layer-set", "parameters": {"index": 9, "name": "Enemy"}},
  {"command": "layer-set", "parameters": {"index": 10, "name": "Projectile"}},
  {"command": "physics-set-collision-matrix", "parameters": {"layer1": 10, "layer2": 10, "ignore": true}}
]'
# 하나라도 실패하면 모든 변경사항이 Undo로 자동 롤백
```

### 빌드 검증 파이프라인

> _"프로젝트 출시 준비 됐는지 확인해줘"_

<p align="center">
  <img src="docs/assets/project-validate.svg" alt="6개 항목을 검사하는 project-validate 출력" width="600">
</p>

```bash
# 실패 원인을 읽고 수정한 뒤 재검증
unityctl gameobject create --name "Main Camera" --project $P
unityctl component add --id "<MainCameraId>" --type "Camera" --project $P
unityctl gameobject set-tag --id "<MainCameraId>" --tag "MainCamera" --project $P
unityctl project validate --project $P --json   # valid: true
```

---

## 첫 번째 쇼케이스 추천

unityctl을 공개적으로 보여주고 싶다면, 마인크래프트부터 시작하지 마세요.
현재 툴체인에서 가장 잘 검증된 루프에 맞는 순서대로 올라가세요:

1. **Zero-to-playable**: 프리미티브, 스크립트, UI, 물리, 검증 결과물로 만드는 작은 3D 아레나 마이크로게임.
2. **버티컬 슬라이스**: 프리팹, NavMesh, 머티리얼, 오디오, 빌드 검증까지 포함된 탑다운 서바이벌 또는 기지 방어 프로토타입.
3. **샌드박스 단계**: 그 다음에 청크 월드, 크래프팅, 절차적 터레인, 세이브 시스템 등으로 확장.

첫 쇼케이스로 가장 좋은 건 오픈월드 샌드박스가 아니라 **작은 3D 서바이벌/기지 방어 게임**입니다.
스크린샷과 GIF로 바로 이해할 수 있고, 씬 편집 + 스크립트 패칭 + 롤백 + 시각 검증 루프에 딱 맞으며, 이후에 더 복잡한 샌드박스로 확장할 수 있습니다.

자세한 내용은 [쇼케이스 로드맵](showcase-roadmap.md)을 참고하세요.

---

## 아키텍처

```
AI 에이전트 (LLM)            unityctl-mcp              unityctl CLI             Unity Editor
Claude / GPT / Gemini         12 MCP 도구               179 명령                 플러그인 (IPC)
        |                          |                          |                       |
        |--- MCP (stdio) -------->|                          |                       |
        |                          |--- CLI 호출 ----------->|                       |
        |                          |                          |--- IPC (~100ms) ---->|
        |                          |                          |    또는 Batch (30s+)  |
        |                          |                          |<--- JSON 응답 -------|
        |                          |<--- 결과 ---------------|                       |
        |<--- 도구 결과 ----------|                          |                       |
```

```
unityctl.slnx
+-- src/Unityctl.Shared   (netstandard2.1)  프로토콜 + 모델
+-- src/Unityctl.Core     (net10.0)         비즈니스 로직
+-- src/Unityctl.Cli      (net10.0)         CLI 셸
+-- src/Unityctl.Mcp      (net10.0)         MCP 서버
+-- src/Unityctl.Plugin   (Unity UPM)       에디터 브릿지 (IPC 서버)
+-- tests/*                                 961 PR .NET xUnit 테스트
```

---

## 플랫폼

| 플랫폼 | CLI | IPC 전송 | Batch | CI |
|--------|-----|----------|-------|----|
| Windows | ✅ | Named Pipe | ✅ | ✅ |
| macOS | ✅ | Unix Domain Socket | ✅ | ✅ |
| Linux | ✅ | Unix Domain Socket | ✅ | ✅ |

## 요구사항

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Unity 2021.3+](https://unity.com/download)

## 터미널 출력

<p align="center">
  <img src="docs/assets/editor-list.svg" alt="unityctl editor list" width="570">
</p>

<p align="center">
  <img src="docs/assets/log-table.svg" alt="unityctl log" width="645">
</p>

<p align="center">
  <img src="docs/assets/tools.svg" alt="unityctl tools — 9개 카테고리 179개 명령" width="654">
</p>


## 실측 토큰 비용

#### 실측: Claude Code 토큰 비용 (2026-03-20)

Claude Code에서 5가지 읽기 전용 QA 작업(컴파일 체크, 씬 계층, 로봇 카탈로그, DH 테이블, 빌드 설정)을 실행했을 때의 **누적 토큰 비용**:

| 스택 | 스키마 (1회) | 5 ops x 1 | 5 ops x 10 |
|---|---:|---:|---:|
| **unityctl via Bash** | **0 tok** | **1,780 tok** | **17,800 tok** |
| unityctl MCP (12 도구) | 1,256 tok | 2,957 tok | 18,261 tok |
| CoplayDev MCP (30 도구) | 11,427 tok | 12,158 tok | 18,742 tok |

핵심:
- **unityctl via Bash는 스키마 오버헤드가 0** — Bash 도구가 이미 Claude Code 시스템 프롬프트에 포함되어 있어 도구 정의에 추가 토큰이 들지 않음
- CoplayDev MCP는 **45 KB**(30개 도구) 스키마를 로드하지만, 5개 QA 작업 중 실제로 대응할 수 있는 건 **1개뿐**
- 일반적인 짧은 세션에서 unityctl via Bash가 CoplayDev MCP 대비 **6.8배 적은 토큰**을 사용
- 전체 벤치마크 방법론과 원시 데이터: [`docs/contest/benchmark-raw/`](../contest/benchmark-raw/)



## Apple Silicon macOS 검증

Apple Silicon MacBook Air에서 Homebrew, .NET SDK `10.0.105`, Unity Hub, Unity `6000.0.64f1` / `6000.3.11f1`로 검증 완료.

검증 경로:

- `dotnet tool install -g unityctl`
- `dotnet tool install -g unityctl-mcp`
- `unityctl editor list`
- `unityctl init --project <project> --source /path/to/unityctl/src/Unityctl.Plugin`
- `unityctl ping --project <project> --json`
- `unityctl doctor --project <project> --json`
- `unityctl status --project <project> --json`
- `unityctl check --project <project> --json`

결과: `ping` → pong 반환, `doctor` → IPC 연결 확인, `status` → Ready, `check` → macOS 통과.

프로젝트 호환성 주의: 프로젝트나 서드파티 패키지가 Unity `6.0 LTS`에 고정되어 있으면, `6000.3+`에서 열었을 때 unityctl과 무관하게 에러가 발생할 수 있습니다. 고정된 `6000.0.64f1`에서 다시 열면 해결됩니다.

## MCP 브릿지 구성도

<p align="center">
  <img src="docs/assets/mcp-demo.svg" alt="AI 에이전트가 MCP를 통해 Unity 씬을 구성하는 모습" width="700">
</p>

