# Getting Started with unityctl

See also: [glossary](./glossary.md)

## Prerequisites

- .NET 10 SDK
- Unity 2021.3+ (via Unity Hub)

## Installation

```bash
git clone https://github.com/your-username/unityctl.git
cd unityctl
dotnet build unityctl.slnx
```

## Architecture

unityctl is organized into three layers:

```
Unityctl.Shared   (netstandard2.1)  Protocol, models, transport interfaces
Unityctl.Core     (net10.0)         Business logic: discovery, transport, retry
Unityctl.Cli      (net10.0)         Thin CLI shell — delegates to Core
Unityctl.Plugin   (Unity UPM)       Editor bridge — runs inside Unity
```

The CLI communicates with Unity via two transport mechanisms:
1. **Batch transport** — spawns Unity in batchmode (always works, slower)
2. **IPC transport** — connects to running Unity Editor via named pipe (Phase 2B, fast)

## Quick Start

### 1. Discover installed Unity Editors

```bash
dotnet run --project src/Unityctl.Cli -- editor list
```

### 2. Initialize a Unity project

```bash
dotnet run --project src/Unityctl.Cli -- init --project "C:/MyGame"
```

This adds the `com.unityctl.bridge` plugin to your project's `Packages/manifest.json`.

### 3. Check project compilation

```bash
dotnet run --project src/Unityctl.Cli -- check --project "C:/MyGame"
```

### 4. Run tests

```bash
dotnet run --project src/Unityctl.Cli -- test --project "C:/MyGame" --mode edit
```

### 5. Build

```bash
dotnet run --project src/Unityctl.Cli -- build --project "C:/MyGame" --target StandaloneWindows64
```

### 6. Discover available tools

```bash
dotnet run --project src/Unityctl.Cli -- tools
dotnet run --project src/Unityctl.Cli -- tools --json
```

The `--json` variant returns a machine-readable JSON array (equivalent to MCP `tools/list`) for AI agent integration.

## JSON Output

All commands support `--json` for machine-readable output:

```bash
dotnet run --project src/Unityctl.Cli -- editor list --json
dotnet run --project src/Unityctl.Cli -- status --project "C:/MyGame" --json
```

## How It Works

### Batch Transport (current)

1. CLI writes a `CommandRequest` JSON to a temp file
2. CLI spawns Unity in batchmode with `-executeMethod`
3. Unity plugin reads the request, executes the command, writes a `CommandResponse` JSON
4. CLI reads the response file and presents results

This avoids the unreliable stdout/exit-code approach of traditional batchmode scripts.

### IPC Transport (Phase 2B)

1. Unity Editor runs an IPC server (named pipe) on startup
2. CLI connects to the pipe, sends `CommandRequest`, receives `CommandResponse`
3. Response time: <200ms (vs 30-120s for batchmode)

The `CommandExecutor` in Core automatically selects the best available transport.

## Running Tests

```bash
# All tests (59 total)
dotnet test unityctl.slnx

# Unit tests only (faster, no CLI execution)
dotnet test unityctl.slnx --filter "FullyQualifiedName!~Integration"

# Specific project
dotnet test tests/Unityctl.Core.Tests
```

Note: Integration tests require the CLI executable to be runnable. On environments with AppLocker, they will skip gracefully.
