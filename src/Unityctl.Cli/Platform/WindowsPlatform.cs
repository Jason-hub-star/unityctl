using Unityctl.Shared.Models;

namespace Unityctl.Cli.Platform;

public sealed class WindowsPlatform : IPlatformServices
{
    public string GetUnityHubEditorsJsonPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "UnityHub", "editors.json");
    }

    public IEnumerable<string> GetDefaultEditorSearchPaths()
    {
        yield return @"C:\Program Files\Unity\Hub\Editor";
        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "Unity", "Hub", "Editor");
    }

    public string GetUnityExecutablePath(string editorBasePath)
        => Path.Combine(editorBasePath, "Editor", "Unity.exe");

    public IEnumerable<UnityProcessInfo> FindRunningUnityProcesses()
    {
        // Phase 1-A에서 구현
        yield break;
    }

    public bool IsProjectLocked(string projectPath)
    {
        var lockFile = Path.Combine(projectPath, "Temp", "UnityLockfile");
        if (!File.Exists(lockFile)) return false;
        try
        {
            using var _ = File.Open(lockFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            return false;
        }
        catch (IOException)
        {
            return true;
        }
    }

    public Stream CreateIpcClientStream(string projectPath)
    {
        // Phase 2에서 Named Pipe 구현
        throw new NotImplementedException("IPC is Phase 2");
    }

    public string GetTempResponseFilePath()
        => Path.Combine(Path.GetTempPath(), $"unityctl-res-{Guid.NewGuid():N}.json");
}
