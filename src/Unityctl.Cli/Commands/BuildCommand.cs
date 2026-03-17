namespace Unityctl.Cli.Commands;

public static class BuildCommand
{
    public static void Execute(string project, string target = "StandaloneWindows64", string? output = null)
    {
        // Phase 1-B에서 구현
        Console.WriteLine($"Build: {Path.GetFullPath(project)} → {target}");
        Console.WriteLine("Build integration — coming in Phase 1-B");
    }
}
