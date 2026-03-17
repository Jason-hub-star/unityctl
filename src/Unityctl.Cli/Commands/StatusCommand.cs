namespace Unityctl.Cli.Commands;

public static class StatusCommand
{
    public static void Execute(string project, bool wait = false)
    {
        // Phase 1-A에서 BatchModeRunner와 연결
        Console.WriteLine($"Status for: {Path.GetFullPath(project)}");
        Console.WriteLine("BatchMode integration — coming in Phase 1-A");
    }
}
