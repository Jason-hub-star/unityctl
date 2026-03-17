# AI Agent Quickstart

This guide is for AI coding agents (Claude, Copilot, etc.) that need to automate Unity projects.

## Zero-config Setup

```bash
# 1. Clone and build
git clone https://github.com/your-username/unityctl.git
cd unityctl
dotnet build src/Unityctl.Cli -c Release

# 2. Add plugin to target Unity project
dotnet run --project src/Unityctl.Cli -- init --project "/path/to/unity/project"

# 3. Verify
dotnet run --project src/Unityctl.Cli -- editor list --json
```

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

## StatusCode Reference

| Code | Name | Meaning | Action |
|------|------|---------|--------|
| 0 | Ready | Success | Done |
| 100-103 | Transient | Unity is busy | Retry (auto with --wait) |
| 200 | NotFound | No Unity installed | Install Unity |
| 201 | ProjectLocked | Editor has project open | Close Editor |
| 203 | PluginNotInstalled | Plugin missing | Run `init` |
| 500+ | Error | Something broke | Check logs |

## Error Recovery

If a command fails, check:
1. `editor list` — is Unity installed?
2. `init --project <path>` — is the plugin installed?
3. Is the project locked by a running Editor?
4. Check the log file path in the error output for Unity logs.
