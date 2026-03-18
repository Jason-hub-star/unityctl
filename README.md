# unityctl

CLI tool for controlling Unity Editor — built for AI agents and CI/CD pipelines.

```bash
# Install plugin + run a build preflight check in 3 commands
dotnet build unityctl.slnx
dotnet run --project src/Unityctl.Cli -- init --project /path/to/unity/project
dotnet run --project src/Unityctl.Cli -- build --dry-run --project /path/to/unity/project --json
```

## Why unityctl?

| Feature | unityctl | Existing Unity MCP |
|---------|----------|--------------------|
| Headless CI/CD | ✅ batch mode, no Editor required | ❌ Editor must be open |
| Editor Discovery | ✅ auto-detect installed versions | ❌ manual path config |
| Transport Fallback | ✅ IPC → batch auto-fallback | ❌ single path |
| Native .NET MCP | ✅ C# SDK, no Python/TS bridge | Python/TS bridge |
| Preflight Validation | ✅ `--dry-run` with 19 checks | ❌ |
| Flight Recorder | ✅ NDJSON command logging | ❌ |
| Session Tracking | ✅ state machine + stale detection | ❌ |

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Unity 2021.3+](https://unity.com/download) via Unity Hub

### Build

```bash
git clone https://github.com/kimjuyoung1127/unityagent.git
cd unityagent
dotnet build unityctl.slnx
```

### Install Plugin

```bash
dotnet run --project src/Unityctl.Cli -- init --project /path/to/unity/project
```

This adds `com.unityctl.bridge` to your Unity project's `Packages/manifest.json`.

### Basic Commands

```bash
# List installed Unity editors
dotnet run --project src/Unityctl.Cli -- editor list --json

# Ping Unity (IPC if Editor open, batch otherwise)
dotnet run --project src/Unityctl.Cli -- ping --project /path/to/project

# Check compilation
dotnet run --project src/Unityctl.Cli -- check --project /path/to/project --json

# Run EditMode tests (with polling)
dotnet run --project src/Unityctl.Cli -- test --project /path/to/project --mode edit --json

# Build preflight validation
dotnet run --project src/Unityctl.Cli -- build --project /path/to/project --target StandaloneWindows64 --dry-run --json

# Build for Windows
dotnet run --project src/Unityctl.Cli -- build --project /path/to/project --target StandaloneWindows64 --json

# View command log
dotnet run --project src/Unityctl.Cli -- log --last 10

# Session management
dotnet run --project src/Unityctl.Cli -- session list --json

# Scene snapshot
dotnet run --project src/Unityctl.Cli -- scene snapshot --project /path/to/project --json

# Execute C# expression in Unity
dotnet run --project src/Unityctl.Cli -- exec --project /path/to/project --code "Application.version"

# Machine-readable schema for AI agents
dotnet run --project src/Unityctl.Cli -- schema --format json
```

## Architecture

```
unityctl.slnx
├── src/Unityctl.Shared   (netstandard2.1)  Protocol + models
├── src/Unityctl.Core     (net10.0)         Business logic (transport, discovery, retry)
├── src/Unityctl.Cli      (net10.0)         Thin CLI shell
├── src/Unityctl.Mcp      (net10.0)         MCP server (Claude/Cursor/VS Code)
├── src/Unityctl.Plugin   (Unity UPM)       Editor bridge
└── tests/*                                 304 xUnit tests
```

### Transport

unityctl auto-selects the best transport:

1. **IPC** (Named Pipe / Unix Domain Socket) — if Unity Editor is running with plugin → ~150ms
2. **Batch** — spawns Unity in batchmode → 30-120s

### MCP Server

```bash
# Run as MCP server (stdio transport)
dotnet run --project src/Unityctl.Mcp
```

Compatible with Claude Code, Cursor, VS Code, and any MCP client.

## Commands

| Command | Description |
|---------|-------------|
| `editor list` | List installed Unity editors |
| `init` | Install plugin to Unity project |
| `ping` | Check Unity connectivity |
| `status` | Get editor state (compiling, playing, etc.) |
| `check` | Verify script compilation |
| `test` | Run EditMode/PlayMode tests |
| `build` | Build player (with `--dry-run` preflight) |
| `log` | Query flight recorder |
| `session` | Manage execution sessions |
| `watch` | Stream Unity events in real-time |
| `scene` | Snapshot and diff scene state |
| `exec` | Execute C# expression in Unity |
| `schema` | Output machine-readable command schema |
| `workflow` | Run JSON workflow files |
| `tools` | List available commands with metadata |

## Status Codes

| Code | Name | Meaning |
|------|------|---------|
| 0 | Ready | Success |
| 100-103 | Transient | Unity is busy (auto-retry) |
| 104 | Accepted | Async operation started |
| 200 | NotFound | Unity not installed |
| 201 | ProjectLocked | Editor has project open (batch) |
| 203 | PluginNotInstalled | Run `init` first |
| 500+ | Error | Check logs |

## Testing

```bash
dotnet test unityctl.slnx                                            # All 304 tests
dotnet test unityctl.slnx --filter "FullyQualifiedName!~Integration" # Unit only
```

## Platforms

| Platform | CLI | IPC | Batch | CI |
|----------|-----|-----|-------|----|
| Windows | ✅ | Named Pipe | ✅ | ✅ |
| macOS | ✅ | Unix Domain Socket | ✅ | ✅ |
| Linux | ✅ | Unix Domain Socket | ✅ | ✅ |

## License

MIT
