# unityctl workflow recipes

## Bridge setup and recovery

```bash
P=/absolute/path/to/project
unityctl doctor --project "$P" --json
unityctl init --project "$P"              # only when the bridge is missing
unityctl await-ready --project "$P" --timeout 300 --json
unityctl status --project "$P" --json
```

If readiness fails, use the `doctor` classification and recommendations. Inspect the Unity Console or Editor log for compilation failures before changing transport code.

## Safe scene and component edit

Read target and scene ownership before writing:

```bash
unityctl editor current --json
unityctl scene hierarchy --project "$P" --summary --json
unityctl gameobject find --project "$P" --name Player --json
unityctl component get --project "$P" --component-id "$COMPONENT_ID" --full --json
```

If an affected scene reports `isDirty: true`, do not write until the user chooses
whether to save or revert the pre-existing changes. If the current selection is a
different project, run `unityctl editor select --project "$P" --json` before writes.

Discover exact flags with `--help`, make one change, then read back the same property:

```bash
unityctl component set-property --project "$P" \
  --component-id "$COMPONENT_ID" --property mass --value 2 --json
unityctl component get --project "$P" \
  --component-id "$COMPONENT_ID" --property m_Mass --json
unityctl scene save --project "$P" --all --json
```

For spatial placement, verify measured geometry rather than relying only on a screenshot:

```bash
unityctl spatial describe --project "$P" --target Player --json
unityctl spatial check --project "$P" --subject Player --predicate on-top-of --target Ground --json
```

Run `--help` first because spatial selector flags can evolve.

## Compile-fix loop

Use normal filesystem editing for C# when possible. Then let Unity compile and return structured errors:

```bash
unityctl script find-refs --project "$P" --symbol TargetSymbol --json  # local; Editor not required
unityctl script validate --project "$P" --wait --json
unityctl script get-errors --project "$P" --json
```

If errors exist, patch only the reported source, validate again, and stop after the first clean compile. Do not guess at multiple fixes while Unity has not recompiled the previous one.

## Project and Play Mode verification

Use the smallest check that proves the change:

```bash
unityctl check --project "$P" --type compile --json
unityctl project validate --project "$P" --json
unityctl test --project "$P" --mode edit --json
```

Treat `data.valid: false` as a failed validation even when an older CLI/bridge
reports transport-level `success: true`. Fix the named failed check and rerun.
For a new project with no enabled build scene:

```bash
unityctl build-settings set-scenes --project "$P" \
  --scenes Assets/Scenes/Main.unity --json
unityctl project validate --project "$P" --json
```

For visual or Play Mode work, create a verification definition outside generated Unity folders:

```json
{
  "name": "agent-smoke",
  "steps": [
    { "id": "validate", "kind": "projectValidate" },
    { "id": "capture", "kind": "capture", "view": "game", "width": 640, "height": 360, "format": "png" },
    { "id": "smoke", "kind": "playSmoke", "durationSeconds": 1, "settleTimeoutSeconds": 10 }
  ]
}
```

```bash
unityctl workflow verify --project "$P" --file ./verify.json \
  --artifacts-dir /tmp/unityctl-evidence --json
```

Use returned artifact paths and hashes as evidence. Avoid `--inline-evidence` unless the caller explicitly needs base64 payloads.

## Multi-instance routing

```bash
unityctl editor instances --json
unityctl editor current --json
unityctl editor select --project "$P" --json
```

When more than one interactive Editor is present, select the intended project/PID
before writes. Continue passing `--project "$P"` to project-scoped commands. Asset
Import Workers and batch processes are not interactive targets even if they share
the project path.
