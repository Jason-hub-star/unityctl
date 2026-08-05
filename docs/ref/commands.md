# unityctl command reference

179 CLI entry points and the 12 MCP tools that load 171 canonical command schemas on demand. Moved out of the README to keep the landing page short — this file is the full surface.

[English](commands.md) | [한국어](commands.ko.md)

---

## Commands (179)

### Core (14)

| Command | Description |
|---------|-------------|
| `ping` | Check Unity connectivity |
| `status` | Editor state (with `--wait` smart polling for Domain Reload) |
| `check` | Verify script compilation (headless) |
| `build` | Build player with `--dry-run` preflight (13 checks) |
| `test` | Run EditMode / PlayMode tests |
| `doctor` | Diagnose connectivity + suggest recovery steps |
| `project validate` | Game readiness check (compile, scenes, camera, lights, console, editor) |
| `init` | Install plugin to Unity project |
| `mcp install` | Write the MCP server entry into a Claude Code / Cursor / VS Code / Codex config |
| `editor list` | Discover installed Unity editors |
| `editor instances` | List running Unity Editor instances |
| `editor current` | Show the selected Unity project target |
| `editor select` | Select a Unity project target, or a unique running PID, for project-less CLI routing |
| `workflow verify` | Run artifact-first verification steps (`projectValidate`, `capture`, `imageDiff`, `consoleWatch`, `uiAssert`, `playSmoke`) |

<details>
<summary><strong>Scene & GameObject</strong> (19)</summary>

| Command | Description |
|---------|-------------|
| `scene snapshot` | Capture scene state |
| `scene hierarchy` | Scene hierarchy tree |
| `scene diff` | Property-level scene diff with epsilon |
| `scene save` | Save active scene |
| `scene open` | Open scene by path |
| `scene create` | Create new scene |
| `gameobject create` | Create GameObject |
| `gameobject delete` | Delete GameObject |
| `gameobject rename` | Rename GameObject |
| `gameobject move` | Reparent GameObject |
| `gameobject find` | Find by name/tag/component |
| `gameobject get` | Get GameObject details |
| `gameobject set-active` | Toggle active state |
| `gameobject set-tag` | Set tag |
| `gameobject set-layer` | Set layer |
| `component add` | Add component |
| `component remove` | Remove component |
| `component get` | Get component properties |
| `component set-property` | Set component property |

</details>

<details>
<summary><strong>Assets & Materials</strong> (21)</summary>

| Command | Description |
|---------|-------------|
| `asset find` | Search by type/label/path |
| `asset get-info` | Asset metadata |
| `asset get-dependencies` | Direct dependencies |
| `asset reference-graph` | Reverse-reference graph |
| `asset create` | Create asset |
| `asset create-folder` | Create folder |
| `asset copy` | Copy asset |
| `asset move` | Move/rename asset |
| `asset delete` | Delete asset |
| `asset import` | Reimport asset |
| `asset refresh` | Refresh AssetDatabase |
| `asset get-labels` | Get labels |
| `asset set-labels` | Set labels |
| `material create` | Create material |
| `material get` | Get material properties |
| `material set` | Set material property |
| `material set-shader` | Change shader |
| `prefab create` | Create prefab from GameObject |
| `prefab unpack` | Unpack prefab instance |
| `prefab apply` | Apply prefab overrides |
| `prefab edit` | Enter/exit prefab edit mode |

</details>

<details>
<summary><strong>Scripting & Code Analysis</strong> (11)</summary>

| Command | Description |
|---------|-------------|
| `script create` | Create C# script from template |
| `script edit` | Replace script content (whole-file) |
| `script patch` | Line-level insert/delete/replace |
| `script delete` | Delete script file |
| `script validate` | Trigger compilation and verify |
| `script list` | List MonoScript assets |
| `script get-errors` | Structured compile errors (file/line/column/code) |
| `script find-refs` | Find symbol references locally without starting Unity |
| `script rename-symbol` | Rename symbol across all scripts (with `--dry-run`) |
| `type describe` | Reflect a live C# type (members, Unity specifics, Manual link); summary-by-default, `--full` for signatures |
| `exec` | Execute C# expression in Unity |
| `exec eval` | Compile & run multi-statement C# via the bundled Roslyn compiler, no domain reload (opt-in: `AllowEval`) |
| `runtime status` / `runtime logs` | Query a running Development Build player (scene, fps, captured logs) over IPC |

</details>

<details>
<summary><strong>Editor Control</strong> (18)</summary>

| Command | Description |
|---------|-------------|
| `play start/stop/pause` | Start, stop, or pause play mode |
| `editor pause` | Toggle editor pause |
| `editor focus-gameview` | Focus Game View |
| `editor focus-sceneview` | Focus Scene View |
| `player-settings get/set` | PlayerSettings read/write |
| `project-settings get/set` | Project settings read/write |
| `console clear` | Clear console |
| `console get-count` | Log/warning/error counts |
| `define-symbols get/set` | Scripting define symbols |
| `tag list/add` | Tag management |
| `layer list/set` | Layer management |
| `undo` | Undo last operation |
| `redo` | Redo last undone operation |

</details>

<details>
<summary><strong>Build & Deployment</strong> (6)</summary>

| Command | Description |
|---------|-------------|
| `build-profile list/get-active/set-active` | Build profile management |
| `build-target switch` | Switch build platform |
| `build-settings get-scenes/set-scenes` | Build scene list |

</details>

<details>
<summary><strong>Physics, Lighting & NavMesh</strong> (12)</summary>

| Command | Description |
|---------|-------------|
| `physics get-settings/set-settings` | DynamicsManager |
| `physics get-collision-matrix/set-collision-matrix` | 32x32 layer collision |
| `lighting bake/cancel/clear` | Lightmap baking |
| `lighting get-settings/set-settings` | Lightmap settings |
| `navmesh bake/clear/get-settings` | NavMesh |

</details>

<details>
<summary><strong>UI & Mesh</strong> (8)</summary>

| Command | Description |
|---------|-------------|
| `ui canvas-create` | Create UI Canvas |
| `ui element-create` | Create Button, Text, Image, etc. |
| `ui set-rect` | Set RectTransform |
| `ui find` | Find UI elements |
| `ui get` | Get UI element details |
| `ui toggle` | Set Toggle state |
| `ui input` | Set InputField text |
| `mesh create-primitive` | Create Cube/Sphere/Plane/Cylinder/Capsule/Quad |

</details>

<details>
<summary><strong>Automation & Monitoring</strong> (15)</summary>

| Command | Description |
|---------|-------------|
| `batch execute` | Transaction with rollback |
| `workflow run` | JSON workflow execution |
| `watch` | Real-time event streaming |
| `log` | Flight recorder query |
| `session list/stop/clean` | Session management |
| `screenshot` | Scene/Game View capture (base64) |
| `schema` / `tools` | Machine-readable metadata |
| `package list/add/remove` | Package management |
| `animation create-clip/create-controller` | Animation assets |

</details>

---

## Selection-aware Routing

```bash
# Pin the current Unity project once
unityctl editor select --project /path/to/project

# Or pin a running Unity PID when it maps to a single project
unityctl editor select --pid 55028

# Inspect the current selection
unityctl editor current --json

# See running Unity instances with PID / project / IPC status
unityctl editor instances --json

# These CLI commands can now omit --project
unityctl ping --json
unityctl status --json
unityctl check --json
unityctl doctor --json

# Run a small verification bundle (artifacts-first)
unityctl workflow verify --file verify.json --project /path/to/project --json
```


## 12 MCP tools

| Tool | Type | Description |
|------|------|-------------|
| `unityctl_query` | Read | Unified read: asset, gameobject, scene, component, UI, physics, lighting, tags |
| `unityctl_run` | Write | Unified write: create, delete, modify, script, material, prefab, batch |
| `unityctl_schema` | Meta | On-demand parameter lookup (by command or category) |
| `unityctl_build` | Action | Build player with 13 preflight checks |
| `unityctl_check` | Action | Compile verification (headless) |
| `unityctl_test` | Action | EditMode / PlayMode tests |
| `unityctl_exec` | Action | Execute arbitrary C# expression |
| `unityctl_status` | Read | Editor state + connectivity |
| `unityctl_ping` | Read | Fast connectivity check |
| `unityctl_watch` | Stream | Real-time console / hierarchy / compilation events |
| `unityctl_log` | Read | Flight recorder query |
| `unityctl_session_list` | Read | Active session list |
