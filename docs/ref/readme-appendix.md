# unityctl README appendix

Worked examples, environment support, and the architecture diagram — moved out of the README to keep the landing page short. Nothing here was deleted; it was relocated.

[English](readme-appendix.md) | [한국어](readme-appendix.ko.md)

---

## What AI Agents Can Build

### Scene Construction

> _"Create a platformer level with a floor, walls, and a player spawn point"_

```bash
# Agent creates scene structure
unityctl scene create --path "Assets/Scenes/Level01.unity" --project $P
unityctl mesh create-primitive --type Plane --name "Floor" --scale "[10,1,10]" --project $P
unityctl mesh create-primitive --type Cube --name "Wall" --position "[5,1,0]" --scale "[0.5,2,10]" --project $P
unityctl gameobject create --name "PlayerSpawn" --project $P
unityctl component add --id "<PlayerSpawnId>" --type "BoxCollider" --project $P

# Agent verifies the scene
unityctl scene hierarchy --project $P --json      # check structure
unityctl screenshot capture --project $P           # visual verification
unityctl project validate --project $P --json      # camera? lights? errors?
```

### Script Authoring with Compile Verification

> _"Write a player movement script and make sure it compiles"_

```bash
# Agent writes code
unityctl script create --path "Assets/Scripts/PlayerMovement.cs" --className "PlayerMovement" --project $P
unityctl script patch --path "Assets/Scripts/PlayerMovement.cs" \
  --startLine 8 --insertContent "public float speed = 5f;" --project $P

# Agent checks compilation — and fixes errors in a loop
unityctl script validate --project $P --wait       # trigger recompile
unityctl script get-errors --project $P --json     # structured CS errors
# if errors: read error, patch fix, validate again
```

### Safe Batch Operations with Rollback

> _"Set up physics layers for Player, Enemy, and Projectile — roll back if anything fails"_

```bash
unityctl batch execute --project $P --rollbackOnFailure true --commands '[
  {"command": "layer-set", "parameters": {"index": 8, "name": "Player"}},
  {"command": "layer-set", "parameters": {"index": 9, "name": "Enemy"}},
  {"command": "layer-set", "parameters": {"index": 10, "name": "Projectile"}},
  {"command": "physics-set-collision-matrix", "parameters": {"layer1": 10, "layer2": 10, "ignore": true}}
]'
# If any command fails, all changes are automatically rolled back via Undo
```

### Build Verification Pipeline

> _"Check if the project is ready to ship"_

<p align="center">
  <img src="docs/assets/project-validate.svg" alt="project-validate output showing 6 checks" width="600">
</p>

```bash
# Agent reads the failure, fixes it, validates again
unityctl gameobject create --name "Main Camera" --project $P
unityctl component add --id "<MainCameraId>" --type "Camera" --project $P
unityctl gameobject set-tag --id "<MainCameraId>" --tag "MainCamera" --project $P
unityctl project validate --project $P --json   # valid: true
```

---

## What To Build First

If you want to prove `unityctl` in public, do not start with Minecraft.
Start with a showcase ladder that matches the strongest verified loops in the current toolchain:

1. **Zero-to-playable**: a small 3D arena microgame built from primitives, scripts, UI, physics, and verification artifacts.
2. **Vertical slice**: expand that into a polished top-down survival or base-defense prototype with prefabs, NavMesh, materials, audio, and build validation.
3. **Sandbox step**: only then move into chunked worlds, crafting, procedural terrain, and save-heavy systems.

The best first showcase for `unityctl` is a **small 3D survival / base-defense game**, not an open-world sandbox.
It is easy to understand in screenshots and GIFs, maps cleanly to scene editing + script patching + rollback + visual verification, and can grow into a more complex sandbox later.

See [Showcase Roadmap](showcase-roadmap.md) for:

- the recommended public demo scope
- the asset checklist to prepare before building
- the pre-production plan for using `unityctl` effectively
- the gaps worth closing before attempting a Minecraft-like demo

---

## Architecture

```
AI Agent (LLM)                unityctl-mcp              unityctl CLI             Unity Editor
Claude / GPT / Gemini         12 MCP tools              179 commands             Plugin (IPC)
        |                          |                          |                       |
        |--- MCP (stdio) -------->|                          |                       |
        |                          |--- CLI invocation ----->|                       |
        |                          |                          |--- IPC (~100ms) ---->|
        |                          |                          |    or Batch (30s+)   |
        |                          |                          |<--- JSON response ---|
        |                          |<--- result -------------|                       |
        |<--- tool result --------|                          |                       |
```

```
unityctl.slnx
+-- src/Unityctl.Shared   (netstandard2.1)  Protocol + models
+-- src/Unityctl.Core     (net10.0)         Business logic
+-- src/Unityctl.Cli      (net10.0)         CLI shell
+-- src/Unityctl.Mcp      (net10.0)         MCP server
+-- src/Unityctl.Plugin   (Unity UPM)       Editor bridge (IPC server)
+-- tests/*                                 961 PR .NET xUnit tests
```

---

## Platforms

| Platform | CLI | IPC Transport | Batch | CI |
|----------|-----|---------------|-------|----|
| Windows | ✅ | Named Pipe | ✅ | ✅ |
| macOS | ✅ | Unix Domain Socket | ✅ | ✅ |
| Linux | ✅ | Unix Domain Socket | ✅ | ✅ |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Unity 2021.3+](https://unity.com/download)

## Terminal Output

<p align="center">
  <img src="docs/assets/editor-list.svg" alt="unityctl editor list" width="570">
</p>

<p align="center">
  <img src="docs/assets/log-table.svg" alt="unityctl log" width="645">
</p>

<p align="center">
  <img src="docs/assets/tools.svg" alt="unityctl tools — 179 commands across 9 categories" width="654">
</p>


## Token cost, measured

#### Measured: Claude Code Token Cost (2026-03-20)

When Claude Code runs 5 read-only QA operations (compile check, scene hierarchy, robot catalog, DH table, build settings), the **cumulative token cost** differs dramatically:

| Stack | Schema (once) | 5 ops x 1 | 5 ops x 10 |
|---|---:|---:|---:|
| **unityctl via Bash** | **0 tok** | **1,780 tok** | **17,800 tok** |
| unityctl MCP (12 tools) | 1,256 tok | 2,957 tok | 18,261 tok |
| CoplayDev MCP (30 tools) | 11,427 tok | 12,158 tok | 18,742 tok |

Key findings:
- **unityctl via Bash has zero schema overhead** — the Bash tool is already in Claude Code's system prompt, so no additional tokens are spent on tool definitions
- CoplayDev MCP loads **45 KB of schemas** (30 tools), but only **1 out of 5** QA operations has a matching tool
- In a typical short session, unityctl via Bash uses **6.8x fewer tokens** than CoplayDev MCP
- Full benchmark methodology and raw data: [`docs/contest/benchmark-raw/`](../contest/benchmark-raw/)



## Apple Silicon macOS Validation

Manual validation was completed on an Apple silicon MacBook Air using Homebrew, .NET SDK `10.0.105`, Unity Hub, and Unity editors `6000.0.64f1` and `6000.3.11f1`.

Validated path:

- `dotnet tool install -g unityctl`
- `dotnet tool install -g unityctl-mcp`
- `unityctl editor list`
- `unityctl init --project <project> --source /path/to/unityctl/src/Unityctl.Plugin`
- `unityctl ping --project <project> --json`
- `unityctl doctor --project <project> --json`
- `unityctl status --project <project> --json`
- `unityctl check --project <project> --json`

Observed result on a Unity `6000.0.64f1` project: `ping` returned `pong`, `doctor` reported IPC connected, `status` returned `Ready`, and `check` passed on macOS.

Project compatibility note: if a Unity project or third-party package is pinned to Unity `6.0 LTS`, opening that same project in `6000.3+` can fail before `unityctl` is involved. During validation, reopening the project in its pinned `6000.0.64f1` editor resolved the project-side render pipeline error.

## How the MCP bridge looks

<p align="center">
  <img src="docs/assets/mcp-demo.svg" alt="AI agent building a Unity scene via MCP" width="700">
</p>

