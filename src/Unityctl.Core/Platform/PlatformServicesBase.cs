using System.IO.Pipes;
using Unityctl.Shared;
using Unityctl.Shared.Models;

namespace Unityctl.Core.Platform;

public abstract class PlatformServicesBase : IPlatformServices
{
    public abstract string GetUnityHubEditorsJsonPath();
    public abstract IEnumerable<string> GetDefaultEditorSearchPaths();
    public abstract string GetUnityExecutablePath(string editorBasePath);
    public abstract IEnumerable<UnityProcessInfo> FindRunningUnityProcesses();

    public virtual bool IsProjectLocked(string projectPath)
    {
        var lockFile = Path.Combine(projectPath, "Temp", "UnityLockfile");
        if (!File.Exists(lockFile))
            return false;

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

    public virtual Stream CreateIpcClientStream(string projectPath)
    {
        var pipeName = Constants.GetPipeName(projectPath);
        var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);
        client.Connect(Constants.IpcConnectTimeoutMs);
        return client;
    }

    public virtual string GetTempResponseFilePath()
        => Path.Combine(Path.GetTempPath(), $"unityctl-res-{Guid.NewGuid():N}.json");
}
