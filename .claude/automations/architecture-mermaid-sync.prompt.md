작업명: unityctl architecture mermaid sync
스케줄: 평일 10:30 (Asia/Seoul)

목표:
- `docs/ref/architecture-mermaid.md`와 현재 코드 구조의 핵심 흐름 정합성 점검

대상:
- `AGENTS.md`
- `docs/ref/architecture-mermaid.md`
- `src/Unityctl.Shared/**`
- `src/Unityctl.Core/**`
- `src/Unityctl.Cli/**`
- `src/Unityctl.Plugin/**`
- `src/Unityctl.Mcp/**`

출력:
- `docs/status/ARCHITECTURE-SYNC-REPORT.md`
- lock: `docs/ref/.architecture-mermaid-sync.lock`

규칙:
- 코드 변경 금지
- 다이어그램과 코드의 불일치만 기록
- 심각도 `HIGH/MEDIUM/LOW`로 구분
- `AGENTS.md`의 폴더 책임 규칙과 불일치도 같이 기록

검사 포인트:
- Shared → Core → Cli / Plugin 소스 복사 의존 방향
- IPC probe-first 흐름
- batch fallback 흐름
- MCP tool surface 존재 여부
- docs에 없는 신규 축(`ExploreTool`, read query handlers) 반영 필요 여부

DRY_RUN:
- `DRY_RUN=true`면 차이 리포트만 출력
- Final line: `[DRY_RUN] no files changed`
