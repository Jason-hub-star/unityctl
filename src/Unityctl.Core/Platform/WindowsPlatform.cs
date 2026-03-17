using Unityctl.Shared.Models;

namespace Unityctl.Core.Platform;

public sealed class WindowsPlatform : PlatformServicesBase
{
    public override string GetUnityHubEditorsJsonPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "UnityHub", "editors.json");
    }

    public override IEnumerable<string> GetDefaultEditorSearchPaths()
    {
        yield return @"C:\Program Files\Unity\Hub\Editor";
        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "Unity", "Hub", "Editor");
    }

    public override string GetUnityExecutablePath(string editorBasePath)
        => Path.Combine(editorBasePath, "Editor", "Unity.exe");

    public override IEnumerable<UnityProcessInfo> FindRunningUnityProcesses()
    {
        // Phase 2B: WMI Win32_Process query
        yield break;
    }

}
