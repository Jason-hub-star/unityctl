# automations/

unityctl automation prompt index for deterministic documentation and status maintenance.

## Principles
- Keep every automation deterministic and idempotent.
- State exact source-of-truth files before comparing or reporting.
- Use lock files to avoid duplicate runs.
- Default to `DRY_RUN=true` and require deliberate promotion to write mode.
- Do not edit `src/` or `tests/` from documentation automations unless the prompt explicitly says to regenerate evidence.

## Prompt Files
| File | Purpose | Schedule |
|---|---|---|
| `docs-status-integrity.prompt.md` | Check command/docs/status alignment and report drift. | 평일 10:00 KST |
| `architecture-mermaid-sync.prompt.md` | Check architecture mermaid docs against current code structure. | 평일 10:30 KST |
| `readme-sync.prompt.md` | README 숫자(커맨드 수/MCP 수/테스트 수)/예시/섹션 drift 감지 및 자동 수정 제안. | 평일 10:45 KST |
| `automation-health-monitor.prompt.md` | Summarize automation lock/report health and stale runs. | 평일 11:00 KST |
| `docs-nightly-organizer.prompt.md` | Roll up daily logs into weekly docs and append nightly run log. | 평일 14:00 KST |

## Execution Order
```text
10:00 docs-status-integrity
10:30 architecture-mermaid-sync
10:45 readme-sync              ← README 정합성
11:00 automation-health-monitor
14:00 docs-nightly-organizer
```

## unityctl-Specific Notes
- Source-of-truth navigation starts at `AGENTS.md`.
- Product/status truth lives in `docs/status/`.
- Reference truth lives in `docs/ref/`.
- Daily logs are evidence only; they must not overwrite current status without explicit promotion.
