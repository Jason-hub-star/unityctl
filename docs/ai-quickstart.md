# AI Agent Quickstart

See also: [glossary](./glossary.md)

This guide is for AI coding agents (Claude, Copilot, etc.) that need to automate Unity projects.

## Zero-config Setup

```bash
# 1. Clone and build
git clone https://github.com/your-username/unityctl.git
cd unityctl
dotnet build unityctl.slnx

# 2. Add plugin to target Unity project
dotnet run --project src/Unityctl.Cli -- init --project "/path/to/unity/project"

# 3. Verify
dotnet run --project src/Unityctl.Cli -- editor list --json
```

## Tool Discovery

Before calling commands, discover what's available:

```bash
# Human-readable list
dotnet run --project src/Unityctl.Cli -- tools

# Machine-readable JSON (MCP tools/list equivalent)
dotnet run --project src/Unityctl.Cli -- tools --json
```

The `--json` output returns a JSON array of tool definitions with name, description, category, and parameter schemas. This is the recommended way for AI agents to dynamically discover available commands.

## Common Workflows

### Check if project compiles

```bash
dotnet run --project src/Unityctl.Cli -- check --project "/path/to/project" --json
# Exit code 0 = success, 1 = failure
```

### Run EditMode tests

```bash
dotnet run --project src/Unityctl.Cli -- test --project "/path/to/project" --mode edit --json
```

### Build for Windows

```bash
dotnet run --project src/Unityctl.Cli -- build --project "/path/to/project" --target StandaloneWindows64 --json
```

## Parameters

Commands accept typed parameters via JSON payload. The CLI maps command-line flags to `JsonObject` parameters internally. Key parameter types:
- **String**: `--target StandaloneWindows64` → `request.GetParam("target")`
- **Bool**: handler-side `request.GetParam<bool>("verbose")`
- **Int**: handler-side `request.GetParam<int>("count")`
- **Nested**: handler-side `request.GetObjectParam("options")`

## StatusCode Reference

| Code | Name | Meaning | Action |
|------|------|---------|--------|
| 0 | Ready | Success | Done |
| 100-103 | Transient | Unity is busy | Retry (auto with --wait) |
| 200 | NotFound | No Unity installed | Install Unity |
| 201 | ProjectLocked | Editor has project open | Close Editor or wait for IPC (Phase 2B) |
| 203 | PluginNotInstalled | Plugin missing | Run `init` |
| 500+ | Error | Something broke | Check logs |

## Transport Selection

The `CommandExecutor` selects transport automatically:
1. **IPC** (Phase 2B): if Unity Editor is running with plugin → <200ms response
2. **Batch**: spawn Unity in batchmode → 30-120s response

Currently only batch transport is implemented. IPC will be added in Phase 2B.

## MCP Compatibility

unityctl is designed as a **superset** of MCP for Unity workflows:

| MCP Feature | unityctl Equivalent | Status |
|-------------|-------------------|--------|
| `tools/list` | `unityctl tools --json` | Available |
| Tool execution | CLI commands + `--json` | Available |
| Prompts | `ai-quickstart.md` | Available |
| Resources | Flight Recorder + Scene Snapshot | Planned (Phase 3B, 4B) |
| Tasks | Session Layer | Planned (Phase 3A) |
| Streaming | Watch Mode | Planned (Phase 3C) |

If MCP bridge is needed, the MCP C# SDK v1.0 `[McpToolType]` attribute can wrap unityctl commands in ~100 lines.

## Error Recovery

If a command fails, check:
1. `editor list` — is Unity installed?
2. `init --project <path>` — is the plugin installed?
3. Is the project locked by a running Editor? (batch transport limitation)
4. Check the log file path in the error output for Unity logs.
