# CTL Feedback

## Summary

- Repeated issues and top improvements are summarized here before final submission.

## Entries

### 2026-03-25

- Phase: Phase 0
- Command: `unityctl package list`, `unityctl doctor`
- Pain Point: package metadata and cached resolution data could lag behind the manifest update, making version verification ambiguous until the Editor was restarted.
- Workaround: restart Unity and re-run `package list`, `doctor`, and inspect `Library/PackageManager/projectResolution.json`.
- Improvement Suggestion: add a dedicated `package resolve` or `doctor --packages` mode that reports manifest target, loaded package version, and stale cache mismatches in one place.
- Severity: medium

### 2026-03-25

- Phase: Phase 3
- Command: `unityctl scene open`, `unityctl ui find`
- Pain Point: after opening one scene, `ui find` could still surface UI from a previously active scene, which made scene-by-scene authored UI verification confusing.
- Workaround: rely on command return payloads, restart Unity when needed, and cross-check with build settings and saved scene paths.
- Improvement Suggestion: add an explicit active-scene assertion to `ui find` or a `--scene` filter for UGUI queries.
- Severity: high

### 2026-03-25

- Phase: Phase 3
- Command: `unityctl console get-entries`
- Pain Point: expected console subcommand naming was easy to guess wrong, and the fallback output was only the generic command list.
- Workaround: inspect `unityctl tools --json` or top-level help before retrying.
- Improvement Suggestion: improve unknown-command guidance with the nearest matching command name.
- Severity: low

### 2026-03-25

- Phase: Phase 6
- Command: `unityctl build`, `unityctl check`, `unityctl status --wait`
- Pain Point: immediately after asset refresh, build/check/status could fail with `103` while IPC was still reloading.
- Workaround: restart the Editor or wait for `Ready` before retrying build-related commands.
- Improvement Suggestion: expose a stronger `await-ready` command that blocks until IPC and compile state are both stable.
- Severity: medium

### 2026-07-21

- Phase: 공식 CLI 벤치마크 (GOAL-unity-cli-benchmark 러닝 A)
- Command: IPC bridge 전체 (도메인 리로드 후)
- Pain Point: 비포커스(백그라운드) 에디터에서 `script create`로 도메인 리로드가 발생하면 IPC 서버가 재기동되지 않음. `UnityctlBootstrap`이 `EditorApplication.delayCall`+`update` 게이트로 시작을 지연하는데, 백그라운드 에디터에서 이 콜백이 흐르지 않아 `ipc-state.json`이 `reloading`에 고착 → 클라이언트가 90초 예산 소진 후 batch 폴백 → 프로젝트 lock 충돌로 141초 만에 실패. 같은 리로드에서 공식 com.unity.pipeline 서버는 `[InitializeOnLoad]` static ctor에서 즉시 재시작해 739ms에 응답(editor3.log 1249행).
- Workaround: 에디터에 포커스를 주면 delayCall이 흘러 브릿지가 살아남.
- Improvement Suggestion: `AssemblyReloadEvents.afterAssemblyReload`에서 직접 재시작하거나(공식 방식), delayCall 게이트에 백그라운드 폴백(예: `EditorApplication.update` 대신 타이머 스레드에서 main-thread dispatch)을 추가. 재기동 실패 시 state 파일에 `stalled` 기록해 클라이언트가 빠르게 진단하도록.
- Severity: high
- **Resolved (2026-07-21, GOAL-unity-cli-benchmark 러닝 C-P0)**: 진범은 delayCall이 GUI 리페인트에 묶여 무인 에디터에서 안 흐르는 것(update는 흐름 — 명령 펌프 정상 동작으로 실증). 플러그인 delayCall 4개소(Bootstrap 시작·IpcServer watch 구독·AssetRefreshHandler 이중 defer)를 `MainThreadDispatch.RunDeferred`(update 기반)로 교체. 라이브 재현: 무인 부팅 기동 10초 내 ready, 리로드 후 자동 재기동(editor5.log stop→start 쌍), T8 시나리오 141,656ms 실패 → 252ms 성공.

### 2026-03-25

- Phase: Phase 3
- Command: `unityctl screenshot capture --view game`
- Pain Point: Game View capture did not include the overlay UGUI authored on Screen Space - Overlay canvases, so visual UI verification via screenshot was misleading.
- Workaround: combine scene hierarchy/UI queries with manual in-editor inspection or change capture strategy instead of trusting the raw game screenshot.
- Improvement Suggestion: support overlay canvas capture in screenshot tooling or clearly document the limitation in the command help.
- Severity: high
