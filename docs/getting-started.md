# Getting Started with unityctl

## Prerequisites

- .NET 10 SDK
- Unity 2021.3+ (via Unity Hub)

## Installation

```bash
git clone https://github.com/your-username/unityctl.git
cd unityctl
dotnet build src/Unityctl.Cli
```

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

## JSON Output

All commands support `--json` for machine-readable output:

```bash
dotnet run --project src/Unityctl.Cli -- editor list --json
dotnet run --project src/Unityctl.Cli -- status --project "C:/MyGame" --json
```

## How It Works

unityctl communicates with Unity via a file-based protocol:

1. CLI writes a `CommandRequest` JSON to a temp file
2. CLI spawns Unity in batchmode with `-executeMethod`
3. Unity plugin reads the request, executes the command, writes a `CommandResponse` JSON
4. CLI reads the response file and presents results

This avoids the unreliable stdout/exit-code approach of traditional batchmode scripts.
