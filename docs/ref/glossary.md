# unityctl Glossary

Key terms used throughout the project, sorted alphabetically.

## A

`Accepted`: StatusCode 104 indicating an async operation has started. The CLI polls for completion automatically.

`AsyncCommandRunner`: Polls for async command results after receiving an Accepted response. Uses delegate injection for testability.

`AsyncOperationRegistry`: Manages async operation state in the Plugin. Provides single-flight guard, age-check, and TTL pruning.

## B

`Batch Execute`: Runs multiple commands in a single IPC round-trip with automatic rollback on failure.

`BatchTransport`: Spawns Unity in batchmode and communicates via request/response files. Works headless without a running Editor.

`Batchmode`: Unity's non-interactive execution mode, used for CI/CD and command-line automation.

## C

`CLI`: The `unityctl` command-line tool that users run from a terminal.

`CommandExecutor`: Core orchestrator that selects transport (IPC or Batch), applies retry policies, and returns the final response.

`CommandRegistry`: Plugin-side dispatcher that maps command names to handler functions.

`CommandRequest`: Request DTO sent from CLI to Unity. Contains command name, parameters, and request ID.

`CommandResponse`: Response DTO returned from Unity. Contains status code, success flag, message, data, and errors.

## D

`Doctor`: Diagnostic command that checks IPC connectivity, plugin health, Editor log errors, and build state.

`Domain Reload`: Unity's process of reloading scripts after code changes. Can temporarily interrupt IPC connections.

`Dry-run`: Execution mode that shows what would happen without making actual changes. Used with `build --dry-run` for preflight validation.

## E

`EventEnvelope`: Common DTO wrapping streaming events (console logs, hierarchy changes, compilation events).

## F

`Flight Recorder`: Append-only NDJSON log that records every command execution with timing, status, and results. Queryable via `unityctl log`.

## G

`Ghost Mode`: Preflight validation mode (`--dry-run`) that checks 19 build conditions without executing the actual build.

## I

`IPC`: Inter-Process Communication between the CLI and a running Unity Editor. Uses Named Pipes (Windows) or Unix Domain Sockets (macOS/Linux).

`IpcServer`: Server running inside the Unity Editor that accepts CLI connections, routes requests to handlers on the main thread, and returns responses.

`IpcTransport`: Transport implementation that connects to a running Unity Editor via IPC. Typical latency ~100ms.

## J

`JsonContext`: System.Text.Json source-generated serialization context used in CLI/Core.

`JsonObject`: Typed JSON node used for structured payload handling. Preferred over `Dictionary<string, object>`.

## M

`MCP`: Model Context Protocol. unityctl implements a native .NET MCP server (`unityctl-mcp`) with 33 tools.

`MCP Server`: The `unityctl-mcp` dotnet tool that exposes unityctl functionality as MCP tools via stdio transport. Compatible with Claude Code, Cursor, VS Code, and any MCP client.

## N

`NDJSON`: Newline-delimited JSON format. Each line is a complete JSON object — used by Flight Recorder and Session Store.

## P

`Plugin`: The `com.unityctl.bridge` UPM package installed into Unity projects. Runs the IPC server and executes commands inside the Editor.

`ProjectLocked`: StatusCode indicating the Unity project is locked by another process. Use IPC transport or close the other Editor.

## R

`Response File`: Communication pattern where Unity writes a JSON response to a temp file instead of stdout, avoiding output pollution in batchmode.

## S

`Scene Diff`: Compares scene snapshots at the property level using SerializedObject traversal and GlobalObjectId tracking. Supports epsilon comparison for floating-point values.

`Session Layer`: Tracks long-running connections to Unity Editors with a 6-state state machine, stale detection, and NDJSON persistence.

`StatusCode`: Enum representing command results: Ready (0), Transient (100-103), Accepted (104), NotFound (200), ProjectLocked (201), PluginNotInstalled (203), Error (500+).

## T

`Transport`: The mechanism for delivering commands to Unity. Either IPC (fast, requires running Editor) or Batch (slow, works headless).

## U

`Undo/Redo`: All write commands register with Unity's Undo system. Supports both direct `unityctl undo/redo` and batch rollback.

`UnityEditorDiscovery`: Component that locates installed Unity Editors and matches project versions.

`UnityProcessDetector`: Detects running Unity processes and determines which projects they have open.

## W

`Watch Mode`: Real-time event streaming via persistent IPC connection. Supports console, hierarchy, and compilation channels.

`Wire Format`: `[4-byte LE length][UTF-8 JSON body]`. Maximum message size: 10 MB.

`Workflow`: JSON-defined command sequence executed via `unityctl workflow run`. Runs commands sequentially with shared context.
