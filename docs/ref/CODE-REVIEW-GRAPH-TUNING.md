# unityctl Code Review Graph Tuning

This document defines a practical tuning loop for `code-review-graph` in `unityctl`.

The goal is not to maximize graph size.
The goal is to keep the graph focused on authored C# code and tests, while preserving canonical docs as repo guidance outside the graph when the parser does not index Markdown in this repository.

## Current Baseline

Verified on 2026-03-25 on Windows with `uvx --from code-review-graph`:

- raw repo file count: `704`
- broad candidate file count (`src`, `tests`, `docs`, root docs): `680`
- graph build result before JS helper exclusion: `396 files`, `2518 nodes`, `5646 edges`
- indexed languages observed: `csharp`, `javascript`

Interpretation:

- the graph is already strongly focused on authored code in this repository
- the main tuning value is noise prevention, not massive file-count reduction
- the remaining non-C# noise came from `scripts/generate-terminal-svg.js`, which is now excluded
- canonical docs remain useful for repo navigation, but they are not expected to appear in the graph DB unless upstream language support changes

## Scope Intent

The tuned graph should center on:

- `src/**`
- `tests/**`

It should avoid surfacing:

- Unity-generated sample project state
- build output and packaging artifacts
- internal logs, scratch work, and automation state
- binary and media files that do not improve structural review context
- helper JavaScript that is unrelated to the runtime command surface

Canonical docs still matter for human navigation:

- `AGENTS.md`
- `README.md`
- `README.ko.md`
- `docs/ref/**`
- `docs/status/**`

Treat them as source-of-truth reading material, not as required graph coverage targets.

## What Good Looks Like

Treat the setup as healthy when these conditions stay true:

- hotspot files are usually under `src/Unityctl.Plugin/Editor/**`
- command-surface hotspots are usually under `src/Unityctl.Cli/Commands/**`
- runtime abstraction hotspots are usually under `src/Unityctl.Core/**`
- agent-surface hotspots are usually under `src/Unityctl.Mcp/Tools/**`
- nearby test projects remain visible for changed runtime areas
- indexed languages settle on `csharp` for the main graph surface

## What Bad Looks Like

Retune `.code-review-graphignore` if you see any of these:

- `docs/internal/**` or `ralph/**` appearing as hotspots
- `.claude/**` or generated sample-project folders dominating indexed files
- binary or media-heavy paths surfacing near the top of node or edge tables
- helper scripts or scratch copies showing up as the only non-C# indexed files
- authored tests disappearing from the indexed surface around major runtime areas
- broad command changes failing to show nearby Shared, Plugin, CLI, and MCP paths together

## Benchmark Scenarios

Use these as the default tuning checks for `unityctl`.

### 1. Shared contract change

Target areas:

- `src/Unityctl.Shared/**`
- `src/Unityctl.Plugin/Editor/Shared/**`

Healthy nearby surface:

- CLI or MCP schema/catalog surfaces
- `tests/Unityctl.Shared.Tests/**`

Why it matters:

- shared protocol changes usually cross the contract, plugin copy, and public command surface together

### 2. CLI command surface change

Target area:

- `src/Unityctl.Cli/Commands/**`

Healthy nearby surface:

- Plugin handlers or registry
- `tests/Unityctl.Cli.Tests/**`

Why it matters:

- command work often spans command parsing, registration, and CLI-facing tests

### 3. MCP tool or schema change

Target area:

- `src/Unityctl.Mcp/Tools/**`

Healthy nearby surface:

- shared catalog or schema surfaces
- `tests/Unityctl.Mcp.Tests/**`

Why it matters:

- MCP shape changes should stay connected to schema-discovery and black-box coverage

### 4. Transport or routing change

Target areas:

- `src/Unityctl.Core/Transport/**`
- `src/Unityctl.Core/Discovery/**`
- `src/Unityctl.Core/EditorRouting/**`
- `src/Unityctl.Core/Platform/**`

Healthy nearby surface:

- `tests/Unityctl.Core.Tests/**`
- `tests/Unityctl.Integration.Tests/**`

Why it matters:

- transport and editor-selection changes are cross-cutting and should keep core plus integration coverage nearby

### 5. Workflow or verification change

Target areas:

- workflow command surface
- shared workflow or verification protocol
- core verification paths

Healthy nearby surface:

- `tests/Unityctl.Cli.Tests/**`

Why it matters:

- workflow changes often bridge CLI orchestration, shared payloads, and verification internals

## Windows Workflow

Build the graph once:

```powershell
uvx --from code-review-graph code-review-graph build
```

Run the repo health report:

```powershell
.\scripts\code_review_graph_report.ps1
```

Refresh the graph first when needed:

```powershell
.\scripts\code_review_graph_report.ps1 -Update
```

If the graph DB is missing and you want the script to build it automatically:

```powershell
.\scripts\code_review_graph_report.ps1 -RebuildIfMissing
```

## Practical Heuristic

- use `rg` for exact text lookup
- use `code-review-graph` for impact lookup
- use the report script to validate index quality and hotspot focus
- expect the report to validate code-heavy graph health, not Markdown coverage
- use tests after the graph narrows the likely blast radius

## Recommended Rule

For `unityctl`, use the graph first when a change crosses Shared contracts, Plugin handlers, CLI commands, MCP tools, transport/routing layers, or workflow verification paths.
Use `rg` after the graph narrows the file set.
