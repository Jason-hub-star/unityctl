# unityctl

[![NuGet](https://img.shields.io/nuget/v/unityctl?label=unityctl)](https://www.nuget.org/packages/unityctl)
[![NuGet](https://img.shields.io/nuget/v/unityctl-mcp?label=unityctl-mcp)](https://www.nuget.org/packages/unityctl-mcp)
[![CI](https://github.com/kimjuyoung1127/unityagent/actions/workflows/ci-dotnet.yml/badge.svg)](https://github.com/kimjuyoung1127/unityagent/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

A deterministic control plane for Unity Editor — built for AI agents and CI/CD pipelines.

**9x smaller schema** than existing Unity MCP solutions. **118 CLI commands**, **33 MCP tools**, **538 tests**.

## Install

```bash
# CLI tool
dotnet tool install -g unityctl

# MCP server (for Claude Code, Cursor, VS Code)
dotnet tool install -g unityctl-mcp
```

## Quick Start

```bash
# Install plugin into Unity project
unityctl init --project /path/to/unity/project

# Ping Unity (IPC if Editor open, batch fallback otherwise)
unityctl ping --project /path/to/project --json

# Check compilation (works headless, no Editor required)
unityctl check --project /path/to/project --json

# Build preflight validation
unityctl build --project /path/to/project --dry-run --json

# MCP server (stdio transport)
unityctl-mcp
```

## Terminal Output

<p align="center">
  <img src="docs/assets/editor-list.svg" alt="unityctl editor list" width="570">
</p>

<p align="center">
  <img src="docs/assets/log-table.svg" alt="unityctl log" width="645">
</p>

<p align="center">
  <img src="docs/assets/tools.svg" alt="unityctl tools" width="654">
</p>

## Why unityctl?

| Feature | unityctl | Existing Unity MCP |
|---------|----------|--------------------|
| Headless CI/CD | ✅ `check` / `test` / `build --dry-run` without Editor | ❌ Editor must be open |
| Token Efficiency | ✅ 5 KB schema (9x smaller) | 45 KB schema |
| Commands | ✅ 118 CLI commands, 70 write actions | ~39 tools |
| Native .NET MCP | ✅ C# SDK, no Python/TS bridge | Python/TS bridge |
| Transport Fallback | ✅ IPC → batch auto-fallback | ❌ single path |
| Preflight Validation | ✅ `--dry-run` with 19 checks | ❌ |
| Flight Recorder | ✅ NDJSON command audit log | ❌ |
| Session Tracking | ✅ state machine + stale detection | ❌ |
| Real-time Streaming | ✅ `watch` console/hierarchy/compilation | ❌ |
| Scene Diff | ✅ property-level diff with epsilon | ❌ |
| Batch Execute | ✅ transaction rollback on failure | ❌ |
| Undo/Redo | ✅ CLI undo/redo support | ❌ |

## Benchmarks

| Metric | unityctl (MCP) | CoplayDev MCP |
|--------|---------------|---------------|
| Schema size | **5,024 B** | 45,705 B |
| `ping` latency | 100 ms | 1 ms |
| `editor_state` | 100 ms | 100 ms |
| `active_scene` | 99 ms | 100 ms |

## Commands (118)

### Core
| Command | Description |
|---------|-------------|
| `editor list` | List installed Unity editors |
| `init` | Install plugin to Unity project |
| `ping` | Check Unity connectivity |
| `status` | Get editor state |
| `check` | Verify script compilation |
| `test` | Run EditMode/PlayMode tests |
| `build` | Build player (with `--dry-run` preflight) |
| `doctor` | Diagnose connectivity and plugin health |

### Scene & GameObject
| Command | Description |
|---------|-------------|
| `scene snapshot/hierarchy/diff/save/open/create` | Scene management |
| `gameobject create/delete/rename/move/find/get` | GameObject CRUD |
| `gameobject set-active/set-tag/set-layer` | GameObject properties |
| `component add/remove/get/set-property` | Component CRUD |

### Assets
| Command | Description |
|---------|-------------|
| `asset find/get-info/get-dependencies/reference-graph` | Asset queries |
| `asset create/copy/move/delete/import/refresh` | Asset CRUD |
| `asset get-labels/set-labels` | Asset labels |
| `material create/get/set/set-shader` | Material management |
| `prefab create/unpack/apply/edit` | Prefab management |

### Editor Control
| Command | Description |
|---------|-------------|
| `play start/stop/pause` | Play mode control |
| `editor pause` | Toggle/set editor pause state |
| `editor focus-gameview/focus-sceneview` | Focus editor windows |
| `player-settings get/set` | PlayerSettings read/write |
| `project-settings get/set` | Project settings (editor, physics, graphics, quality) |
| `console clear/get-count` | Console management |
| `define-symbols get/set` | Scripting define symbols |
| `undo/redo` | Undo/redo operations |

### Build & Deployment
| Command | Description |
|---------|-------------|
| `build-profile list/get-active/set-active` | Build profile management |
| `build-target switch` | Switch build platform |
| `build-settings get-scenes/set-scenes` | Build scene list |

### Physics & Lighting
| Command | Description |
|---------|-------------|
| `physics get-settings/set-settings` | DynamicsManager settings |
| `physics get-collision-matrix/set-collision-matrix` | 32×32 layer collision matrix |
| `lighting bake/cancel/clear/get-settings/set-settings` | Lightmap baking |
| `navmesh bake/clear/get-settings` | NavMesh baking |

### Tags & Layers
| Command | Description |
|---------|-------------|
| `tag list/add` | Tag management |
| `layer list/set` | Layer management |

### Scripting
| Command | Description |
|---------|-------------|
| `script create/edit/delete/validate` | C# script management |
| `script list` | List MonoScript assets |
| `exec` | Execute C# expression in Unity |

### Automation
| Command | Description |
|---------|-------------|
| `batch execute` | Transaction with rollback |
| `workflow run` | JSON workflow execution |
| `watch` | Real-time event streaming |
| `log` | Query flight recorder |
| `session list/stop/clean` | Session management |
| `screenshot capture` | Scene/Game View capture |
| `schema/tools` | Machine-readable metadata |
| `package list/add/remove` | Package management |
| `animation create-clip/create-controller` | Animation assets |
| `ui canvas-create/element-create/set-rect` | UI creation |

## Architecture

```
unityctl.slnx
├── src/Unityctl.Shared   (netstandard2.1)  Protocol + models
├── src/Unityctl.Core     (net10.0)         Business logic (transport, discovery, retry)
├── src/Unityctl.Cli      (net10.0)         CLI shell → dotnet tool "unityctl"
├── src/Unityctl.Mcp      (net10.0)         MCP server → dotnet tool "unityctl-mcp"
├── src/Unityctl.Plugin   (Unity UPM)       Editor bridge (IPC server)
└── tests/*                                 538 xUnit tests
```

### Transport

unityctl auto-selects the best transport:

1. **IPC** (Named Pipe / Unix Domain Socket) — Editor running with plugin → ~100ms
2. **Batch** — spawns Unity in batchmode → 30-120s

### MCP Server

```bash
unityctl-mcp
```

33 MCP tools: `unityctl_run` (70 write commands), `unityctl_schema`, `unityctl_asset_find`, `unityctl_gameobject_find`, `unityctl_script_list`, `unityctl_physics_get_settings`, and more.

Compatible with Claude Code, Cursor, VS Code, and any MCP client.

## Testing

```bash
dotnet test unityctl.slnx                                            # All 538 tests
dotnet test unityctl.slnx --filter "FullyQualifiedName!~Integration" # Unit only
```

## Platforms

| Platform | CLI | IPC | Batch | CI |
|----------|-----|-----|-------|----|
| Windows | ✅ | Named Pipe | ✅ | ✅ |
| macOS | ✅ | Unix Domain Socket | ✅ | ✅ |
| Linux | ✅ | Unix Domain Socket | ✅ | ✅ |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Unity 2021.3+](https://unity.com/download)

## License

MIT
