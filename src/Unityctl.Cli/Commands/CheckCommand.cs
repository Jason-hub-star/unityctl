namespace Unityctl.Cli.Commands;

public static class CheckCommand
{
    public static void Execute(string project, string type = "compile")
    {
        // Phase 1-B에서 구현
        Console.WriteLine($"Check {type}: {Path.GetFullPath(project)}");
        Console.WriteLine("Check integration — coming in Phase 1-B");
    }
}
