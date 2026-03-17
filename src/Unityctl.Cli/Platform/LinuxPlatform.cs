using Unityctl.Shared.Models;

namespace Unityctl.Cli.Platform;

public sealed class LinuxPlatform : IPlatformServices
{
    public string GetUnityHubEditorsJsonPath()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".config", "UnityHub", "editors.json");
    }

    public IEnumerable<string> GetDefaultEditorSearchPaths()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        yield return Path.Combine(home, "Unity", "Hub", "Editor");
    }

    public string GetUnityExecutablePath(string editorBasePath)
        => Path.Combine(editorBasePath, "Editor", "Unity");

    public IEnumerable<UnityProcessInfo> FindRunningUnityProcesses()
    {
        // Linux는 제한적 지원 — /proc/pid/cmdline 파싱 예정
        yield break;
    }

    public bool IsProjectLocked(string projectPath)
    {
        var lockFile = Path.Combine(projectPath, "Temp", "UnityLockfile");
        return File.Exists(lockFile);
    }

    public Stream CreateIpcClientStream(string projectPath)
    {
        throw new NotImplementedException("IPC is Phase 2");
    }

    public string GetTempResponseFilePath()
        => Path.Combine(Path.GetTempPath(), $"unityctl-res-{Guid.NewGuid():N}.json");
}
