# AI Agent Quickstart

This guide is for AI coding agents (Claude, Copilot, etc.) that need to automate Unity projects via unityctl.

## Setup

### 1. Install

```bash
dotnet tool install -g unityctl
dotnet tool install -g unityctl-mcp   # MCP server
```

### 2. Install plugin into Unity project

```bash
unityctl init --project "/path/to/unity/project"
```

Open (or restart) the Unity Editor after running this command.

### 3. Verify

```bash
unityctl editor list --json
unityctl ping --project "/path/to/project" --json
```

## MCP Server Setup

### Claude Code

Add to your MCP configuration:

```json
{
  "mcpServers": {
    "unityctl": {
      "command": "unityctl-mcp",
      "args": []
    }
  }
}
```

### Cursor / VS Code

Add to your MCP settings:

```json
{
  "mcpServers": {
    "unityctl": {
      "command": "unityctl-mcp"
    }
  }
}
```

The MCP server exposes 33 tools including `unityctl_run` (70 write commands via allowlist), `unityctl_schema`, `unityctl_asset_find`, `unityctl_gameobject_find`, `unityctl_screenshot_capture`, and more.

## Tool Discovery

```bash
# Human-readable list
unityctl tools

# Machine-readable JSON (all 118 commands with parameter schemas)
unityctl tools --json

# Schema for a specific command
unityctl schema --command "gameobject create" --json
```

AI agents should call `unityctl tools --json` or the MCP `unityctl_schema` tool to dynamically discover available commands and their parameters.

## Common Workflows

### Read project state

```bash
unityctl status --project "/path/to/project" --json
unityctl check --project "/path/to/project" --json
unityctl scene hierarchy --project "/path/to/project" --json
```

### Search and inspect

```bash
# Find assets
unityctl asset find --project "/path/to/project" --filter "t:Prefab" --json

# Find GameObjects
unityctl gameobject find --project "/path/to/project" --name "Player" --json

# Get component properties
unityctl component get --project "/path/to/project" --target "Main Camera" --component "Camera" --json

# View dependency graph
unityctl asset reference-graph --project "/path/to/project" --path "Assets/Prefabs/Player.prefab" --json
```

### Create and modify

```bash
# Create a GameObject
unityctl gameobject create --project "/path/to/project" --name "Enemy" --json

# Add a component
unityctl component add --project "/path/to/project" --target "Enemy" --component "BoxCollider" --json

# Set a component property
unityctl component set-property --project "/path/to/project" --target "Enemy" --component "BoxCollider" --property "m_Size" --value "[2,2,2]" --json

# Save scene
unityctl scene save --project "/path/to/project" --json
```

### Script management

```bash
# Create a new script
unityctl script create --project "/path/to/project" --name "EnemyAI" --json

# Edit a script (whole-file replace)
unityctl script edit --project "/path/to/project" --path "Assets/Scripts/EnemyAI.cs" --file ./EnemyAI.cs --json

# Validate compilation
unityctl script validate --project "/path/to/project" --json

# List scripts
unityctl script list --project "/path/to/project" --folder Assets --json
```

### Build and test

```bash
# Preflight validation
unityctl build --project "/path/to/project" --dry-run --json

# Build
unityctl build --project "/path/to/project" --target StandaloneWindows64 --json

# Run tests
unityctl test --project "/path/to/project" --mode edit --json
```

### Batch edit with rollback

```bash
unityctl batch execute --project "/path/to/project" --file ./batch.json --json
```

Sends multiple commands in one IPC round-trip. If any step fails, completed steps are rolled back automatically.

Rollback coverage:
- **Undo-backed**: `gameobject-*`, `component-*`, `ui-*`, `material-set`, `material-set-shader`, `player-settings`, `project-settings set`, `prefab unpack`
- **Compensation-backed**: `asset-create`, `asset-copy`, `asset-move`

### Scene diff

```bash
# Take a snapshot
unityctl scene snapshot --project "/path/to/project" --json

# Compare with live state
unityctl scene diff --project "/path/to/project" --live --json
```

### Screenshot (MCP)

The `unityctl_screenshot_capture` MCP tool captures Scene or Game View as base64 PNG/JPG — useful for visual verification in AI workflows.

## StatusCode Reference

| Code | Name | Meaning | Action |
|------|------|---------|--------|
| 0 | Ready | Success | Done |
| 100-103 | Transient | Unity is busy | Retry automatically |
| 104 | Accepted | Async operation started | Poll for result |
| 200 | NotFound | No Unity installed | Install Unity |
| 201 | ProjectLocked | Project locked | Close Editor or use IPC |
| 203 | PluginNotInstalled | Plugin missing | Run `unityctl init` |
| 500+ | Error | Internal error | Check `doctor` output |

## Transport

unityctl auto-selects transport:

1. **IPC** — connects to running Editor via Named Pipe (Windows) or Unix Domain Socket (macOS/Linux). ~100ms latency.
2. **Batch** — spawns Unity in batchmode. 30-120s. Works headless in CI/CD.

## Error Recovery

If a command fails:

1. `unityctl doctor --project <path> --json` — diagnose IPC, plugin, and Editor state
2. `unityctl ping --project <path>` — verify Editor connectivity
3. `unityctl init --project <path>` — reinstall plugin if missing
4. Check the Unity Editor log path shown in error output
