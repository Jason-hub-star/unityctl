using Unityctl.Shared.Models;

namespace Unityctl.Cli.Platform;

public sealed class MacOsPlatform : IPlatformServices
{
    public string GetUnityHubEditorsJsonPath()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, "Library", "Application Support", "UnityHub", "editors.json");
    }

    public IEnumerable<string> GetDefaultEditorSearchPaths()
    {
        yield return "/Applications/Unity/Hub/Editor";
    }

    public string GetUnityExecutablePath(string editorBasePath)
        => Path.Combine(editorBasePath, "Unity.app", "Contents", "MacOS", "Unity");

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
        // Phase 2에서 Unix Domain Socket 구현
        throw new NotImplementedException("IPC is Phase 2");
    }

    public string GetTempResponseFilePath()
        => Path.Combine(Path.GetTempPath(), $"unityctl-res-{Guid.NewGuid():N}.json");
}
