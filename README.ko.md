# unityctl

[English](README.md) | [한국어](README.ko.md)

[![NuGet](https://img.shields.io/nuget/v/unityctl?label=unityctl)](https://www.nuget.org/packages/unityctl)
[![NuGet](https://img.shields.io/nuget/v/unityctl-mcp?label=unityctl-mcp)](https://www.nuget.org/packages/unityctl-mcp)
[![CI](https://github.com/Jason-hub-star/unityctl/actions/workflows/ci-dotnet.yml/badge.svg)](https://github.com/Jason-hub-star/unityctl/actions/workflows/ci-dotnet.yml)
[![Unity Integration](https://github.com/Jason-hub-star/unityctl/actions/workflows/ci-unity.yml/badge.svg)](https://github.com/Jason-hub-star/unityctl/actions/workflows/ci-unity.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

### AI가 게임을 만들 수 있게 해주는 실행 레이어.

AI 에이전트에 **179개 명령**을 쥐여주세요. Unity 씬 구성부터 C# 스크립트 작성, 빌드 검증, 게임 배포까지 — 문제가 생기면 자동으로 롤백됩니다.

```
179 CLI 명령 · 12 MCP 도구 · 961 PR .NET 테스트 · Windows / macOS / Linux
```

<p align="center">
  <img src="docs/assets/unityctl-demo.gif" alt="unityctl 라이브 세션: status, scene hierarchy, mesh create-primitive, gameobject find, screenshot capture — 모든 응답이 out/ 에 아티팩트로 기록됨" width="820">
</p>

<p align="center"><em>실제 Unity 6000.3.16f1 에디터에 붙은 무편집 세션 — 모든 명령이 구조화 JSON으로 답하고 <code>out/</code>에 아티팩트를 남깁니다.</em></p>

공식 Unity CLI(1.0.0-beta.2 + com.unity.pipeline)와 같은 에디터 세션에서 정면 벤치마크 — 더 빠른 왕복, 더 작은 응답, 그리고 측정된 격차는 당일 전부 흡수. [벤치마크 문서](docs/contest/benchmark-vs-unity-cli.md) 참조.

| 실측 (같은 에디터, 같은 태스크) | unityctl v0.6.0 | 공식 Unity CLI |
|---|---|---|
| 씬 계층 읽기 | **286 ms / 919 B** | 617 ms / 1,602 B |
| play 진입 → 콘솔 → 정지 | **965 ms** | 2,588 ms |
| 다중 문장 C# eval | **1,755 ms** (opt-in 게이트) | 2,634 ms (상시 활성) |
| 도메인 리로드 생존 (무인) | **313–516 ms** | 739 ms |
| 무인 테스트 실행 | **1 passed (4.2 s)** | 가짜 성공 — 0개 실행 |
| 잘못된 인자 | 명시적 실패 + 후보 목록 | 조용히 무시 후 success 반환 |
| 카메라 없는 씬 스크린샷 | 뷰 캡처 성공 | 실패 |

품질 게이트: 모든 PR에서 .NET Shared/Core/Cli/Mcp 테스트를 Windows, macOS, Linux에서 실행합니다. Unity Editor가 필요한 검증은 Unity Integration workflow로 분리하고, nightly/manual 실행에서 `init`, 샘플 프로젝트 `doctor`, `check`, `scene hierarchy`, `player-settings set/get`, `workflow verify` 증거를 artifact로 업로드합니다. Unity Integration에는 `UNITY_LICENSE` 또는 `UNITY_SERIAL` GitHub secret이 필요합니다.

기여자는 [CONTRIBUTING.md](CONTRIBUTING.md)에서 테스트 신뢰 체크리스트, flaky 테스트 정책, 명령 동기화 체크리스트, Unity live validation 분리 기준을 확인하세요.

---

## 문제

AI 에이전트는 코드는 잘 쓰는데, **게임은 못 만듭니다.** Unity에 씬 편집이나 에셋 관리, 프로젝트 검증을 위한 프로그래밍 인터페이스가 없기 때문입니다.

기존 Unity MCP 서버들이 이 문제를 해결하려 했지만, 오히려 새로운 문제를 만들었습니다:

| 문제점 | AI 에이전트에 미치는 영향 |
|---|---|
| 매 턴 **45 KB 이상의 스키마** 로드 | 추론 대신 도구 정의에 토큰을 낭비 |
| **검증 피드백이 없음** | 변경 후 씬이 깨졌는지 알 방법이 없음 |
| **롤백이 불가능** | 명령 하나 잘못하면 프로젝트 상태가 오염됨 |
| **Play Mode에서 WebSocket이 끊김** | Unity Domain Reload 과정에서 연결이 끊어짐 |
| **에디터가 반드시 열려 있어야 함** | GUI 없는 CI/CD 환경에서 사용 불가 |

## 해결

unityctl은 Unity Editor를 프로그래밍 가능한 API로 만드는 **.NET CLI + MCP 서버**입니다.

에이전트가 명령을 _실행_하는 것에 그치지 않고, 결과를 _검증_하고, 실패를 _진단_하고, 실수를 _복구_할 수 있는 **폐루프 자동화**를 제공합니다:

<p align="center">
  <img src="docs/assets/agent-loop.svg" alt="계획 - 실행 - 검증 - 진단 루프" width="680">
</p>

> **다른 도구는 에이전트에게 손만 줍니다. unityctl은 손, 눈, 그리고 안전망까지 줍니다.**

---

## 왜 unityctl인가?

| | unityctl | 기존 Unity MCP |
|---|---|---|
| **스키마 오버헤드** | 세션당 **5 KB** (9배 작음) | 매 턴 45 KB 이상 |
| **검증 루프** | `project validate` + `scene diff` + `screenshot capture` | 에이전트가 결과를 확인할 수 없음 |
| **에러 복구** | `script get-errors`로 파일/줄/열/에러코드까지 구조화 | 콘솔 로그 원문 그대로이거나 아예 없음 |
| **안전한 실험** | `batch execute --rollbackOnFailure` + `undo` | 롤백 없음 — 실수가 그대로 남음 |
| **연결 안정성** | Named Pipe — Domain Reload에서도 끊기지 않음 | WebSocket 끊김, 수동 재연결 필요 |
| **CI/CD** | `check` / `test` / `build --dry-run` 헤드리스 지원 | 에디터를 열어야만 동작 |
| **진단** | `doctor`가 실패를 분류하고 다음 조치를 안내 | "Connection failed"만 출력 |
| **명령 수** | **179** (읽기 + 쓰기 + 검증 + 진단) | ~34-200 |
| **감사 추적** | 모든 명령의 NDJSON 플라이트 레코더 | 이력 없음 |
| **런타임** | 네이티브 .NET — Python/TS 브릿지 불필요 | 브릿지 오버헤드 있음 |
| **설치** | `dotnet tool install -g unityctl` | Node.js + npm + 포트 설정 |
| **라이선스** | **MIT** | 다양 |

### 토큰 효율성

AI 에이전트 비용의 대부분은 매 턴 전송되는 도구 스키마에서 발생합니다. unityctl은 **온디맨드 스키마 로딩**으로 이 비용을 극적으로 줄입니다:

<p align="center">
  <img src="docs/assets/token-efficiency.svg" alt="실측 토큰 비용: unityctl via Bash = 오버헤드 0, CoplayDev MCP 대비 6.8배 저렴" width="620">
</p>

CLI는 편의 래퍼를 포함해 179개 진입점을 제공합니다. 12개 MCP 도구는
`unityctl_query`, `unityctl_run`, `unityctl_schema`를 통해 정규화된 171개
명령 스키마를 필요할 때만 불러와 프롬프트 크기를 줄입니다.

---

## 설치

**단독 실행 바이너리 — .NET SDK 불필요** (권장):

```bash
# macOS (Apple Silicon) — 필요에 따라 unityctl-osx-x64 / unityctl-linux-x64 로 교체
curl -L https://github.com/Jason-hub-star/unityctl/releases/latest/download/unityctl-osx-arm64.tar.gz | tar xz
./unityctl --version
```

Windows: [Releases](https://github.com/Jason-hub-star/unityctl/releases/latest)에서 `unityctl-win-x64.zip`을 받아 압축을 풉니다.
각 아카이브에는 self-contained `unityctl` + `unityctl-mcp` 실행 파일과 내장 Unity 플러그인 템플릿이 들어 있습니다.

**또는 .NET tool로** (.NET 10 SDK 필요):

```bash
dotnet tool install -g unityctl
dotnet tool install -g unityctl-mcp
```

Claude Code와 Codex용 에이전트 워크플로 스킬(선택):

```bash
npx skills add Jason-hub-star/unityctl \
  --skill unityctl-workflows \
  -a claude-code -a codex
```

이 스킬은 에이전트가 실제 명령 목록을 먼저 탐색하고, 올바른 Unity 프로젝트를
선택하며, 모든 변경을 구조화된 재조회와 검증으로 마무리하도록 안내합니다.

참고:
- `--source`에 로컬 `Unityctl.Plugin` 폴더 경로나 Git URL을 넣을 수 있습니다: `https://github.com/Jason-hub-star/unityctl.git?path=/src/Unityctl.Plugin#v0.6.3`

## 빠른 시작

```bash
# 1. 에디터 플러그인 설치
unityctl init --project /path/to/project \
  --source "https://github.com/Jason-hub-star/unityctl.git?path=/src/Unityctl.Plugin#v0.6.3"

# 2. Unity Editor에서 프로젝트를 열고 연결 확인
unityctl ping --project /path/to/project --json
unityctl status --project /path/to/project --json

# 3. 만들기 시작
unityctl gameobject create --name "Player" --project /path/to/project
unityctl component add --id "<PlayerId>" --type "Rigidbody" --project /path/to/project
unityctl scene save --project /path/to/project

# 4. 검증
unityctl project validate --project /path/to/project --json

# 5. 빌드
unityctl build --project /path/to/project --dry-run    # 13개 사전 검증
```

### MCP 설정 (AI 에이전트)

클라이언트당 명령 하나면 됩니다 — 기존 설정을 덮어쓰지 않고 병합합니다:

```bash
unityctl mcp install --client claude-code            # 또는 cursor / codex
unityctl mcp install --client vscode --project .     # VS Code는 워크스페이스 단위
unityctl mcp install --client cursor --dry-run       # 미리보기, 쓰지 않음
```

직접 넣으려면:

```json
{
  "mcpServers": {
    "unityctl": {
      "command": "unityctl-mcp"
    }
  }
}
```

---

## 문서

- [명령어 레퍼런스](docs/ref/commands.ko.md) — 179개 CLI 명령과 12개 MCP 도구 전체
- [README 부록](docs/ref/readme-appendix.ko.md) — 실사용 예제, 아키텍처, 플랫폼 지원
- [시작하기](docs/ref/getting-started.md) — 설치, 설정, 주요 워크플로우
- [AI 에이전트 빠른 시작](docs/ref/ai-quickstart.md) — MCP 설정 및 에이전트 연동 가이드
- [쇼케이스 로드맵](docs/ref/showcase-roadmap.md) — 추천 데모 구성, 에셋 체크리스트, 사전 작업 계획
- [아키텍처](docs/ref/architecture-mermaid.md) — 시스템 설계 및 전송 계층 다이어그램
- [용어 사전](docs/ref/glossary.md) — 주요 용어와 개념

## 변경 이력

버전 이력은 [GitHub Releases](https://github.com/Jason-hub-star/unityctl/releases)를 확인하세요.

## 라이선스

MIT — [LICENSE](LICENSE) 참고
