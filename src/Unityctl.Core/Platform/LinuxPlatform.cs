using Unityctl.Shared.Models;

namespace Unityctl.Core.Platform;

public sealed class LinuxPlatform : PlatformServicesBase
{
    public override string GetUnityHubEditorsJsonPath()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".config", "UnityHub", "editors.json");
    }

    public override IEnumerable<string> GetDefaultEditorSearchPaths()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        yield return Path.Combine(home, "Unity", "Hub", "Editor");
    }

    public override string GetUnityExecutablePath(string editorBasePath)
        => Path.Combine(editorBasePath, "Editor", "Unity");

    public override IEnumerable<UnityProcessInfo> FindRunningUnityProcesses()
    {
        if (!OperatingSystem.IsLinux() || !Directory.Exists("/proc"))
            return [];

        var processes = new List<UnityProcessInfo>();
        try
        {
            foreach (var processDirectory in Directory.EnumerateDirectories("/proc"))
            {
                if (!int.TryParse(Path.GetFileName(processDirectory), out var processId))
                    continue;

                try
                {
                    var executable = File.ResolveLinkTarget(
                        Path.Combine(processDirectory, "exe"),
                        returnFinalTarget: true)?.FullName;
                    var commandLine = File.ReadAllBytes(
                        Path.Combine(processDirectory, "cmdline"));
                    var process = TryParseProcess(processId, executable, commandLine);
                    if (process != null)
                        processes.Add(process);
                }
                catch (IOException)
                {
                    // Process exited or became unreadable between /proc enumeration and read.
                }
                catch (UnauthorizedAccessException)
                {
                    // Another user's process can be hidden by procfs permissions.
                }
            }
        }
        catch
        {
            return [];
        }

        return processes;
    }

    internal static UnityProcessInfo? TryParseProcess(
        int processId,
        string? executablePath,
        ReadOnlySpan<byte> commandLine)
    {
        if (string.IsNullOrWhiteSpace(executablePath)
            || !string.Equals(Path.GetFileName(executablePath), "Unity", StringComparison.Ordinal)
            || !string.Equals(
                Path.GetFileName(Path.GetDirectoryName(executablePath)),
                "Editor",
                StringComparison.Ordinal))
        {
            return null;
        }

        var arguments = System.Text.Encoding.UTF8.GetString(commandLine)
            .Split('\0', StringSplitOptions.RemoveEmptyEntries);
        string? projectPath = null;
        for (var index = 0; index < arguments.Length - 1; index++)
        {
            if (string.Equals(arguments[index], "-projectPath", StringComparison.OrdinalIgnoreCase))
            {
                projectPath = arguments[index + 1];
                break;
            }
        }

        var isBatchMode = arguments.Any(argument =>
            string.Equals(argument, "-batchmode", StringComparison.OrdinalIgnoreCase)
            || string.Equals(argument, "-nographics", StringComparison.OrdinalIgnoreCase)
            || string.Equals(argument, "-adb2", StringComparison.OrdinalIgnoreCase));
        var editorDirectory = Path.GetDirectoryName(executablePath);

        return new UnityProcessInfo
        {
            ProcessId = processId,
            ProjectPath = projectPath,
            Version = Directory.GetParent(editorDirectory ?? string.Empty)?.Name,
            ExecutablePath = executablePath,
            IsBatchMode = isBatchMode,
            HasMainWindow = !isBatchMode,
            CommandLineSource = string.Join(' ', arguments)
        };
    }
}
