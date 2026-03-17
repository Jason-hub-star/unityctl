using Unityctl.Shared.Models;

namespace Unityctl.Core.Platform;

public sealed class MacOsPlatform : PlatformServicesBase
{
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
        // Phase 2B: ps aux | grep Unity
        yield break;
    }

}
