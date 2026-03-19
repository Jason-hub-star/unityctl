# automation-health-monitor - automation lock and report health

## Meta
- Task: unityctl automation health summary
- Schedule: 평일 11:00 (Asia/Seoul)
- Role: Check lock files, stale reports, and failed/nightly automation residue
- Project root: `C:\Users\ezen601\Desktop\Jason\unityctl`

## Validation Surface
- `.claude/automations/**`
- `docs/status/*.lock`
- `docs/ref/*.lock`
- `docs/status/*REPORT*.md`
- `docs/status/*HISTORY*.ndjson`
- `docs/status/NIGHTLY-RUN-LOG.md`

## Lock
- Lock file: `docs/status/.automation-health-monitor.lock`
- On start write `{"status":"running","started_at":"<ISO>"}`
- On finish write `{"status":"released","released_at":"<ISO>"}`

## Procedure
1. Acquire the lock.
2. Enumerate all expected automation prompts from `.claude/automations/CLAUDE.md`.
3. Check for stale lock files:
   - `running` older than 2 hours => `STALE_LOCK`
   - malformed JSON => `BROKEN_LOCK`
4. Check whether each report file exists and has a recent timestamp.
5. Check whether `NIGHTLY-RUN-LOG.md` exists and appended successfully after the latest weekday run.
6. Print summary counts.
7. If not `DRY_RUN`, write `docs/status/AUTOMATION-HEALTH-REPORT.md`.
8. Release the lock.

## Must Not
- Do not auto-delete lock files.
- Do not auto-rewrite reports.
- Only summarize health and stale state.

## DRY_RUN=true
- Print health summary only.
- Final line: `[DRY_RUN] no files changed`
