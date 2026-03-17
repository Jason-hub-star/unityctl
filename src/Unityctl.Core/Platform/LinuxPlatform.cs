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
        // Phase 2B: /proc/pid/cmdline parsing
        yield break;
    }

}
