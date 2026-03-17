# Phase 실행 보드

상태값: `Ready | InProgress | QA | Done | Hold`

| phase | module | priority | status | must_read_docs | last_updated |
|-------|--------|----------|--------|----------------|--------------|
| Phase 0 | 프로젝트 골격 | P0 | Done | CLAUDE.md | 2026-03-17 |
| Phase 0.5 | Plugin Bootstrap | P0 | Done | CLAUDE.md | 2026-03-17 |
| Phase 1A | CLI 기본 (Discovery, BatchMode, Retry) | P0 | Done | docs/ref/architecture-mermaid.md | 2026-03-17 |
| Phase 1B | 핵심 기능 (Build, Test, Check Handler) | P0 | Done | docs/ref/code-patterns.md | 2026-03-17 |
| Phase 1C | 테스트 + 배포 | P1 | Hold | .github/workflows/ | 2026-03-17 |
| Phase 2A | Foundation (Payload, Core 추출) | P0 | Done | docs/ref/architecture-mermaid.md | 2026-03-17 |
| Phase 2A+ | Tools Metadata | P0 | Done | docs/ref/phase-roadmap.md | 2026-03-17 |
| Phase 2B | IPC Transport (Named Pipe) | P0 | Done | docs/ref/phase-2b-plan.md | 2026-03-17 |
| Phase 2C | Async Commands (TestHandler) | P0 | Done | docs/ref/phase-roadmap.md | 2026-03-18 |
| Phase 3A | Session Layer | P1 | Ready | docs/ref/phase-roadmap.md | 2026-03-17 |
| Phase 3B | Flight Recorder | P1 | Ready | docs/ref/phase-roadmap.md | 2026-03-17 |
| Phase 3C | Watch Mode (Streaming) | P1 | Ready | docs/ref/phase-roadmap.md | 2026-03-17 |
| Phase 4A | Ghost Mode (--dry-run) | P2 | Ready | docs/ref/phase-roadmap.md | 2026-03-17 |
| Phase 4B | Scene Diff | P2 | Ready | docs/ref/phase-roadmap.md | 2026-03-17 |
| Phase 5 | Agent Layer (JSON 워크플로우) | P2 | Ready | docs/ref/phase-roadmap.md | 2026-03-17 |

## Zero-Drift 규칙
1. `src/` 구조를 코드 모듈 Source of Truth로 간주한다.
2. 보드의 phase/module 상태와 `docs/status/PROJECT-STATUS.md`는 동기화한다.
3. Phase 완료 시 `CLAUDE.md`의 Phase Status 테이블도 갱신한다.
4. Transport 설계 변경은 `docs/ref/phase-2b-plan.md` (또는 해당 phase 문서) 에 반영한다.
5. 테스트 수 변경 시 `PROJECT-STATUS.md`와 `CLAUDE.md` 양쪽 갱신한다.
