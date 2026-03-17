namespace Unityctl.Cli.Commands;

public static class TestCommand
{
    public static void Execute(string project, string mode = "edit", string? filter = null)
    {
        // Phase 1-B에서 구현
        Console.WriteLine($"Test: {Path.GetFullPath(project)} mode={mode}");
        Console.WriteLine("Test integration — coming in Phase 1-B");
    }
}
