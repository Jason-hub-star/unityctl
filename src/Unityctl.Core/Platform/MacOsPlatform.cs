using System.Diagnostics;
using System.Text.RegularExpressions;
using Unityctl.Shared.Models;

namespace Unityctl.Core.Platform;

public sealed class MacOsPlatform : PlatformServicesBase
{
    private static readonly Regex UnityProcessRegex = new(
        @"^(?<executable>.+?/Unity\.app/Contents/MacOS/Unity)(?:\s|$)",
        RegexOptions.Compiled);

    private static readonly Regex ProjectPathRegex = new(
        @"(?:^|\s)-projectPath\s+(?<path>.+?)(?=\s+-[\w]|$)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public override string GetUnityHubEditorsJsonPath()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, "Library", "Application Support", "UnityHub", "editors.json");
    }

    public override IEnumerable<string> GetDefaultEditorSearchPaths()
    {
        yield return "/Applications/Unity/Hub/Editor";
    }

    public override string GetUnityExecutablePath(string editorBasePath)
        => Path.Combine(editorBasePath, "Unity.app", "Contents", "MacOS", "Unity");

    public override IEnumerable<UnityProcessInfo> FindRunningUnityProcesses()
    {
        if (!OperatingSystem.IsMacOS())
            return [];

        try
        {
            var startInfo = new ProcessStartInfo("/bin/ps")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            startInfo.ArgumentList.Add("-axo");
            startInfo.ArgumentList.Add("pid=,command=");

            using var process = Process.Start(startInfo);
            if (process == null)
                return [];

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return process.ExitCode == 0 ? ParseProcessList(output) : [];
        }
        catch
        {
            return [];
        }
    }

    internal static IReadOnlyList<UnityProcessInfo> ParseProcessList(string output)
    {
        var processes = new List<UnityProcessInfo>();
        foreach (var rawLine in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.TrimStart();
            var separator = line.IndexOf(' ');
            if (separator <= 0 || !int.TryParse(line[..separator], out var processId))
                continue;

            var commandLine = line[(separator + 1)..].TrimStart();
            var unityMatch = UnityProcessRegex.Match(commandLine);
            if (!unityMatch.Success)
                continue;

            var projectMatch = ProjectPathRegex.Match(commandLine);
            var projectPath = projectMatch.Success
                ? projectMatch.Groups["path"].Value.Trim().Trim('"', '\'')
                : null;
            var executablePath = unityMatch.Groups["executable"].Value;
            var isBatchMode = ContainsSwitch(commandLine, "-batchmode")
                              || ContainsSwitch(commandLine, "-nographics")
                              || ContainsSwitch(commandLine, "-adb2");

            processes.Add(new UnityProcessInfo
            {
                ProcessId = processId,
                ProjectPath = projectPath,
                Version = TryParseVersion(executablePath),
                ExecutablePath = executablePath,
                IsBatchMode = isBatchMode,
                HasMainWindow = !isBatchMode,
                CommandLineSource = commandLine
            });
        }

        return processes;
    }

    private static string? TryParseVersion(string executablePath)
    {
        var appDirectory = Directory.GetParent(
            Directory.GetParent(
                Directory.GetParent(executablePath)?.FullName ?? string.Empty)?.FullName
            ?? string.Empty);
        return appDirectory?.Parent?.Name;
    }

    private static bool ContainsSwitch(string commandLine, string switchName)
        => commandLine.Contains(switchName, StringComparison.OrdinalIgnoreCase);
}
