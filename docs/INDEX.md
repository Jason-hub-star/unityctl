# INDEX — 문서 지도

이 저장소의 문서를 찾아가는 단일 진입점. 세션 시작 시 `docs/SESSION-START.md`를 먼저 읽는다.

## 폴더 규약

**`docs/` 루트에는 `INDEX.md`와 `SESSION-START.md`만 둔다.** `scripts/check/docs.sh`가 강제한다.

| 폴더 | 담는 것 | 분류 |
|---|---|---|
| `ref/` | 코드와 사용법의 근거가 되는 문서 | SSOT |
| `status/` | 현재 상태·백로그·피드백 | 상태 |
| `goals/` | 골 브리프 — 완료 판정 기준이 여기 있다 | 계약 |
| `contest/` | 공모전 제출·포지셔닝 산출물 | 조사 |
| `internal/` | 내부 기록. **사용자용 근거로 쓰지 않는다** | 내부 |
| `internal/benchmark/` | 벤치마크 원본과 주장 감사 | 증거 |
| `internal/ralph/phases/` · `daily/` · `weekly/` | 진행 로그. 개별 등재하지 않는다 | 내부 |

## 문서 목록

| 경로 | 역할 | 분류 |
|---|---|---|
| `CLAUDE.md` | Claude 진입 문서. 시작 순서·아키텍처·기술 스택·Phase 현황 | SSOT |
| `AGENTS.md` | Codex 진입 문서. CLAUDE.md와 같은 정책 | SSOT |
| `README.md` | 깃허브 첫 화면. 설치·명령 레퍼런스 | SSOT |
| `README.ko.md` | 한국어판 README | SSOT |
| `CONTRIBUTING.md` | 기여 절차 | SSOT |
| `docs/SESSION-START.md` | 세션 진입 캡슐. 하드 룰 6개 | SSOT |
| `docs/INDEX.md` | 문서 지도 | SSOT |
| `docs/ref/architecture-mermaid.md` | 구조 다이어그램 — **의존 방향의 정본** | SSOT |
| `docs/ref/code-patterns.md` | 코드 규약 — **코드를 쓰기 전 필수** | SSOT |
| `docs/ref/phase-roadmap.md` | Phase 순서와 완료 조건 | SSOT |
| `docs/ref/getting-started.md` | 처음 쓰는 사람용 안내 | SSOT |
| `docs/ref/ai-quickstart.md` | 에이전트가 unityctl을 쓰는 법 | SSOT |
| `docs/ref/commands.md` | 명령어 레퍼런스 (en) — 179 CLI + 12 MCP, README에서 이관 | SSOT |
| `docs/ref/commands.ko.md` | 명령어 레퍼런스 (ko) | SSOT |
| `docs/ref/readme-appendix.md` | README 부록 (en) — 예제·아키텍처·플랫폼 | SSOT |
| `docs/ref/readme-appendix.ko.md` | README 부록 (ko) | SSOT |
| `docs/ref/glossary.md` | 용어 | SSOT |
| `docs/ref/phase-2b-plan.md` | IPC Transport 상세 계획 | SSOT |
| `docs/ref/showcase-roadmap.md` | 쇼케이스 로드맵 | 조사 |
| `docs/ref/competitive-analysis-2026-07-29.md` | 경쟁 제품 분석 | 조사 |
| `docs/ref/feature-summary-post.md` | 기능 요약 (홍보용 초안) | 조사 |
| `docs/ref/CODE-REVIEW-GRAPH-TUNING.md` | 코드리뷰 그래프 튜닝 기록 | 조사 |
| `docs/status/PROJECT-STATUS.md` | **현재 상태의 정본** | 상태 |
| `docs/status/PHASE-EXECUTION-BOARD.md` | Phase 실행 보드 | 상태 |
| `docs/status/FEATURE-BACKLOG.md` | 기능 백로그 | 상태 |
| `docs/status/CTL-FEEDBACK.md` | 사용 피드백 수집 | 상태 |
| `docs/status/DOGFOOD-VAMPIRE-SURVIVORS.md` | 자체 사용 기록 (뱀서류 프로젝트) | 상태 |
| `docs/status/README-SYNC-REPORT.md` | README 동기화 점검 결과 | 상태 |
| `docs/goals/GOAL-spatial-grounding.md` | 공간 사실 판정(`spatial`) 골 브리프 | 계약 |
| `docs/goals/GOAL-unity-cli-benchmark.md` | 공식 CLI 벤치마크 골 브리프 | 계약 |
| `docs/contest/2026-oss-developer-contest.md` | 2026 OSS 개발자 공모전 제출 | 조사 |
| `docs/contest/positioning-vs-unity-cli.md` | 공식 Unity CLI 대비 포지셔닝 | 조사 |
| `docs/contest/benchmark-vs-unity-cli.md` | 공식 CLI 대비 벤치마크 | 조사 |
| `docs/contest/demo/ROBOT-DEMO-PRODUCTION.md` | 데모 제작 절차 | 조사 |
| `docs/contest/demo/SHOTLIST.md` | 데모 샷 리스트 | 조사 |
| `docs/internal/DEVELOPMENT.md` | 빌드·테스트·릴리스 절차 | 내부 |
| `docs/internal/CAPTURE-GUIDE.md` | 화면 캡처 절차 | 내부 |
| `docs/internal/phase-history.md` | Phase 이력 원문 | 내부 |
| `docs/internal/research.md` · `root-research.md` | 조사 노트 | 내부 |
| `docs/internal/ref-CLAUDE.md` · `status-CLAUDE.md` | 폴더별 작업 규칙 | 내부 |
| `docs/internal/benchmark/benchmark-results.md` | 벤치마크 실측 원본 | 증거 |
| `docs/internal/benchmark/competitive-claims-audit.md` | **주장 감사 — 수치를 인용하기 전에 본다** | 증거 |
| `docs/internal/benchmark/headless-batch-validation.md` | headless 배치 검증 | 증거 |
| `docs/internal/benchmark/token-comparison.md` | 토큰 비교 | 증거 |
| `docs/internal/benchmark/readme-benchmark.md` | README 게재용 벤치마크 | 증거 |
| `docs/internal/plans/FEATURE-SUGGESTIONS.md` | 기능 제안 모음 | 내부 |
| `docs/internal/plans/competitive-improvement-plan-2026-03-19.md` | 경쟁력 개선 계획 | 내부 |
| `docs/internal/plans/verification-workflow-v1-2026-03-19.md` | 검증 워크플로 v1 | 내부 |
| `docs/internal/tasks/benchmark-vs-mcp.md` | MCP 대비 벤치마크 과제 | 내부 |

`internal/ralph/phases/` · `internal/daily/` · `internal/weekly/` 는 진행 로그다.
개별 등재하지 않고 필요할 때 폴더를 연다.

## 분류 기준

- **SSOT**: 코드나 결정의 유일한 근거로 삼는다
- **상태**: 현재 진행 상황과 막힌 지점
- **계약**: 골 브리프. 완료 판정 기준이 여기 있다
- **증거**: 실측 결과. 수치를 인용할 때 원본
- **조사**: 사전 탐색·대외 산출물. SSOT가 아니다
- **내부**: 내부 기록. 사용자용 근거로 쓰지 않는다

## 새 문서 추가 규칙

1. 위 표에 한 줄 등재한다.
2. `docs/` 루트에 두지 않는다 — 폴더를 고른다.
3. 벤치마크 수치를 담으면 `internal/benchmark/`에 두고 분류를 증거로 적는다.
