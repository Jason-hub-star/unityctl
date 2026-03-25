from __future__ import annotations

import argparse
import os
import shutil
import sqlite3
import subprocess
import sys
from pathlib import Path


PROJECT_COVERAGE_TARGETS = (
    "docs/ref/architecture-mermaid.md",
    "docs/status/PROJECT-STATUS.md",
    "src/Unityctl.Shared/Protocol/WellKnownCommands.cs",
    "src/Unityctl.Shared/Commands/CommandCatalog.cs",
    "src/Unityctl.Core/Transport/CommandExecutor.cs",
    "src/Unityctl.Core/EditorRouting/EditorTargetResolver.cs",
    "src/Unityctl.Cli/Commands/StatusCommand.cs",
    "src/Unityctl.Cli/Commands/SceneCommand.cs",
    "src/Unityctl.Mcp/Tools/StatusTool.cs",
    "src/Unityctl.Plugin/Editor/Commands/StatusHandler.cs",
    "src/Unityctl.Plugin/Editor/Commands/BatchExecuteHandler.cs",
    "tests/Unityctl.Shared.Tests/CommandCatalogTests.cs",
    "tests/Unityctl.Cli.Tests/StatusCommandTests.cs",
    "tests/Unityctl.Cli.Tests/GameObjectCommandTests.cs",
    "tests/Unityctl.Core.Tests/Transport/IpcTransportWatchTests.cs",
    "tests/Unityctl.Mcp.Tests/McpBlackBoxTests.cs",
)


def eprint(message: str) -> None:
    print(message, file=sys.stderr)


def normalize_path(value: str, repo_root: Path) -> str:
    raw = value.replace("\\", "/")
    repo_prefix = repo_root.as_posix().rstrip("/") + "/"
    if raw.startswith(repo_prefix):
        raw = raw[len(repo_prefix) :]
    return raw.lstrip("./")


def print_section(title: str) -> None:
    print()
    print(title)


def bucket_for_path(path: str) -> str:
    if path.startswith("src/Unityctl.Plugin/Editor/Commands/"):
        return "Plugin Commands"
    if path.startswith("src/Unityctl.Plugin/Editor/Utilities/"):
        return "Plugin Utilities"
    if path.startswith("src/Unityctl.Plugin/"):
        return "Plugin Other"
    if path.startswith("src/Unityctl.Cli/Commands/"):
        return "CLI Commands"
    if path.startswith("src/Unityctl.Cli/"):
        return "CLI Other"
    if path.startswith("src/Unityctl.Core/Transport/"):
        return "Core Transport"
    if path.startswith("src/Unityctl.Core/Discovery/"):
        return "Core Discovery"
    if path.startswith("src/Unityctl.Core/EditorRouting/"):
        return "Core EditorRouting"
    if path.startswith("src/Unityctl.Core/Platform/"):
        return "Core Platform"
    if path.startswith("src/Unityctl.Core/"):
        return "Core Other"
    if path.startswith("src/Unityctl.Shared/Protocol/"):
        return "Shared Protocol"
    if path.startswith("src/Unityctl.Shared/Models/"):
        return "Shared Models"
    if path.startswith("src/Unityctl.Shared/"):
        return "Shared Other"
    if path.startswith("src/Unityctl.Mcp/Tools/"):
        return "MCP Tools"
    if path.startswith("src/Unityctl.Mcp/"):
        return "MCP Other"
    if path.startswith("tests/Unityctl.Cli.Tests/"):
        return "CLI Tests"
    if path.startswith("tests/Unityctl.Core.Tests/"):
        return "Core Tests"
    if path.startswith("tests/Unityctl.Shared.Tests/"):
        return "Shared Tests"
    if path.startswith("tests/Unityctl.Mcp.Tests/"):
        return "MCP Tests"
    if path.startswith("tests/Unityctl.Integration.Tests/"):
        return "Integration Tests"
    if path.startswith("tests/Unityctl.Integration/"):
        return "Integration Sample"
    if path.startswith("tests/"):
        return "Other Tests"
    if path.startswith("docs/ref/"):
        return "Docs Ref"
    if path.startswith("docs/status/"):
        return "Docs Status"
    if path in {"AGENTS.md", "README.md", "README.ko.md"}:
        return "Root Canonical Docs"
    if path.startswith("docs/"):
        return "Other Docs"
    return "Other"


def require_command(name: str) -> None:
    if shutil.which(name) is None:
        raise RuntimeError(
            f"Required command not found: {name}. Install it and retry."
        )


def graph_command_parts(raw_command: str) -> list[str]:
    return [part for part in raw_command.split(" ") if part]


def run_graph_status(repo_root: Path, graph_command: list[str]) -> str:
    env = os.environ.copy()
    env["PYTHONUTF8"] = "1"
    env["PYTHONIOENCODING"] = "utf-8"

    result = subprocess.run(
        [*graph_command, "status"],
        cwd=repo_root,
        capture_output=True,
        text=True,
        check=False,
        env=env,
    )
    if result.returncode != 0:
        details = result.stderr.strip() or result.stdout.strip() or "unknown error"
        raise RuntimeError(f"code-review-graph status failed: {details}")
    return result.stdout.strip()


def count_repo_surface_files(repo_root: Path) -> int:
    if shutil.which("rg") is not None:
        result = subprocess.run(
            ["rg", "--files", "."],
            cwd=repo_root,
            capture_output=True,
            text=True,
            check=False,
        )
        if result.returncode == 0:
            return sum(1 for line in result.stdout.splitlines() if line.strip())

    return sum(1 for path in repo_root.rglob("*") if path.is_file())


def query_file_counts(connection: sqlite3.Connection, table_name: str) -> dict[str, int]:
    cursor = connection.execute(
        f"""
        SELECT file_path, COUNT(*) AS item_count
        FROM {table_name}
        GROUP BY file_path
        ORDER BY item_count DESC, file_path ASC
        """
    )
    return {row[0]: row[1] for row in cursor.fetchall() if row[0]}


def merged_file_stats(
    node_counts: dict[str, int], edge_counts: dict[str, int], repo_root: Path
) -> dict[str, dict[str, int]]:
    merged: dict[str, dict[str, int]] = {}
    all_paths = set(node_counts) | set(edge_counts)
    for raw_path in all_paths:
        normalized = normalize_path(raw_path, repo_root)
        merged[normalized] = {
            "nodes": node_counts.get(raw_path, 0),
            "edges": edge_counts.get(raw_path, 0),
        }
    return merged


def print_status_section(status_text: str) -> None:
    print_section("Graph status")
    if status_text:
        print(status_text)
    else:
        print("(no status output)")


def print_reduction_summary(repo_root: Path, file_stats: dict[str, dict[str, int]]) -> None:
    raw_repo_files = count_repo_surface_files(repo_root)
    indexed_files = len(file_stats)
    reduction_ratio = 0.0
    if raw_repo_files > 0:
        reduction_ratio = 100 - ((indexed_files / raw_repo_files) * 100)

    print_section("Repository reduction summary")
    print(f"{raw_repo_files:>6} raw repo files")
    print(f"{indexed_files:>6} indexed graph files")
    print(f"{reduction_ratio:>5.1f}% reduction from raw repo surface")


def print_scope_summary(file_stats: dict[str, dict[str, int]]) -> None:
    buckets: dict[str, dict[str, int]] = {}
    for path, stats in file_stats.items():
        bucket = bucket_for_path(path)
        current = buckets.setdefault(bucket, {"files": 0, "nodes": 0, "edges": 0})
        current["files"] += 1
        current["nodes"] += stats["nodes"]
        current["edges"] += stats["edges"]

    rows = sorted(
        buckets.items(),
        key=lambda item: (-item[1]["files"], -item[1]["nodes"], item[0]),
    )

    print_section("Indexed scope summary")
    for bucket, stats in rows:
        print(
            f"{stats['files']:>4} files  {stats['nodes']:>6} nodes  "
            f"{stats['edges']:>6} edges  {bucket}"
        )


def print_top_files(
    title: str, file_stats: dict[str, dict[str, int]], key: str, limit: int
) -> None:
    rows = sorted(
        file_stats.items(),
        key=lambda item: (-item[1][key], item[0]),
    )
    print_section(title)
    for path, stats in rows[:limit]:
        print(f"{stats[key]:>6}  {path}")


def print_project_coverage(file_stats: dict[str, dict[str, int]]) -> None:
    indexed_paths = set(file_stats)
    print_section("Project coverage checks")
    for target in PROJECT_COVERAGE_TARGETS:
        present = "indexed" if target in indexed_paths else "missing"
        print(f"[{present}] {target}")


def print_interpretation(file_stats: dict[str, dict[str, int]]) -> None:
    hotspot_paths = {
        path
        for path, stats in sorted(
            file_stats.items(),
            key=lambda item: (-(item[1]["nodes"] + item[1]["edges"]), item[0]),
        )[:15]
    }
    noisy_prefixes = (
        "docs/internal/",
        "ralph/",
        ".claude/",
        "tests/Unityctl.Integration/SampleUnityProject/Library/",
        "tests/Unityctl.Integration/SampleUnityProject/Logs/",
        "tests/Unityctl.Integration/SampleUnityProject/Temp/",
        "tests/Unityctl.Integration/SampleUnityProject/UserSettings/",
    )
    noisy_hits = [path for path in hotspot_paths if path.startswith(noisy_prefixes)]
    has_docs = any(
        path.startswith(("docs/ref/", "docs/status/")) or path in {"AGENTS.md", "README.md", "README.ko.md"}
        for path in file_stats
    )
    languages = {Path(path).suffix.lower() or "<none>" for path in file_stats}

    print_section("Quick interpretation")
    print("- Healthy reports keep hotspots centered on Plugin, CLI, Core, MCP, and test projects.")
    if ".js" in languages:
        print("- JavaScript is still indexed. If that is unexpected, check helper-script exclusions.")
    else:
        print("- Indexed language surface is effectively C# only, which matches the main unityctl runtime.")
    if has_docs:
        print("- Docs are appearing in the graph DB. Re-check expectations before treating that as noise.")
    else:
        print("- Canonical docs remain repo guidance, but are not currently part of the graph DB on this setup.")
    if noisy_hits:
        print("- Retune .code-review-graphignore: unexpected noisy hotspots detected.")
        for path in noisy_hits:
            print(f"  {path}")
    else:
        print("- No obvious noisy hotspot prefixes detected in the top combined file set.")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Windows-friendly code-review-graph health report for unityctl."
    )
    parser.add_argument("--repo-root", required=True, help="Repository root path")
    parser.add_argument("--db-path", required=True, help="Path to .code-review-graph/graph.db")
    parser.add_argument("--top", type=int, default=10, help="Number of top files to print")
    parser.add_argument(
        "--graph-command",
        default="code-review-graph",
        help="Command prefix used to invoke code-review-graph, for example 'uvx --from code-review-graph code-review-graph'",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    repo_root = Path(args.repo_root).resolve()
    db_path = Path(args.db_path).resolve()
    graph_command = graph_command_parts(args.graph_command)

    try:
        require_command("python")
        require_command(graph_command[0])

        if not db_path.is_file():
            raise RuntimeError(
                f"Graph database not found at {db_path}. Run 'code-review-graph build' first."
            )

        status_text = run_graph_status(repo_root, graph_command)

        connection = sqlite3.connect(db_path)
        try:
            node_counts = query_file_counts(connection, "nodes")
            edge_counts = query_file_counts(connection, "edges")
        finally:
            connection.close()

        file_stats = merged_file_stats(node_counts, edge_counts, repo_root)

        print("unityctl code-review-graph report")
        print(f"repo: {repo_root}")
        print(f"db:   {db_path}")
        print(f"tool: {' '.join(graph_command)}")

        print_status_section(status_text)
        print_reduction_summary(repo_root, file_stats)
        print_scope_summary(file_stats)
        print_top_files("Top files by node count", file_stats, "nodes", args.top)
        print_top_files("Top files by edge count", file_stats, "edges", args.top)
        print_project_coverage(file_stats)
        print_interpretation(file_stats)
        return 0
    except RuntimeError as exc:
        eprint(str(exc))
        return 1
    except sqlite3.Error as exc:
        eprint(f"SQLite query failed: {exc}")
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
