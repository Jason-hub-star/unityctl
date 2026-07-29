---
name: unityctl-workflows
description: Drive, inspect, debug, and verify Unity projects through the unityctl CLI or MCP server. Use when an agent needs to modify scenes, GameObjects, components, assets, scripts, project settings, Play Mode, tests, builds, screenshots, or verification workflows in a Unity project, especially when changes need structured JSON readback, rollback, diagnostics, or artifact evidence.
---

# unityctl Workflows

Use `unityctl` as the execution and evidence layer. Keep source-code edits in normal filesystem tools; use unityctl for Unity-owned state and live Editor operations.

## Establish the target

1. Resolve the Unity project root containing `Assets/`, `Packages/`, and `ProjectSettings/`.
2. Pass `--project` explicitly on every project-scoped command. Discovery commands
   such as `editor current` and `editor instances` are global and do not accept it.
3. Check the installed CLI and project:

```bash
unityctl --version
unityctl doctor --project "$P" --json
```

4. If the bridge is missing, install the bundled bridge, then open Unity and wait for stability:

```bash
unityctl init --project "$P"
unityctl await-ready --project "$P" --timeout 300 --json
```

Do not run `init` again when `doctor` reports a healthy matching bridge.

## Discover, do not guess

Use the live command surface instead of memorizing flags:

```bash
unityctl tools --json
unityctl <command> --help
```

`tools --json` returns a top-level array. Use names returned by it; the machine
command catalog uses hyphenated names while the CLI may expose grouped verbs.

```bash
unityctl tools --json | jq '.[].name'
```

## Run the closed loop

1. Read the current state, including `scene hierarchy --summary`.
2. If any affected scene is already dirty, stop before writing. Ask whether to
   save the existing work or let the user revert it; do not mix ownership.
3. Compare `editor current --json` with the intended project. Before writes,
   select the intended target explicitly when they differ:

```bash
unityctl editor current --json
unityctl editor select --project "$P" --json
```

4. Make the smallest Unity-owned change.
5. Read back the exact changed field or object.
6. Save the affected scene or asset.
7. Run the narrowest relevant check.
8. Produce artifact evidence for visual or multi-step work.

Prefer stable `globalObjectId` values after saving a scene. Name targeting is acceptable for initial creation but is ambiguous once duplicates exist.

## Safety rules

- Prefer `batch execute` with rollback for related writes.
- Use a command's dry-run option when its help exposes one.
- Do not hand-edit `.unity`, `.prefab`, `.asset`, or ProjectSettings YAML while an Editor-backed command covers the operation.
- Do not use `exec eval` unless the project explicitly enables it and the requested operation lacks a typed command.
- Treat 1xx status codes as transient, 2xx as precondition/fatal, and 5xx as command failures. Run `doctor --json` before retrying blindly.
- During compilation or domain reload, use `await-ready`; do not spawn a second Editor against a locked project.
- If healthy IPC reads conflict with process inventory or target metadata, record
  `unityctl --version` and its install source. Upgrade only when the task permits
  environment changes; otherwise recommend it and request approval. Reads may
  continue through explicit project routing, but unresolved routing contradictions block writes.
- A local `file:` bridge is expected when its path is inside the unityctl source
  checkout being developed. Otherwise treat it as a consumer-project drift risk,
  not as a failed connection by itself.
- Keep screenshots/base64 out of ordinary responses. Prefer `workflow verify` artifact paths and hashes.

## Choose a workflow

Read [references/workflows.md](references/workflows.md) only for the relevant path:

- bridge setup and health recovery
- safe scene/component editing
- compile-fix loops
- Play Mode or visual verification
- multi-instance routing

Finish with the exact commands run, their pass/fail result, artifact paths, and any Unity-only validation that remains.
