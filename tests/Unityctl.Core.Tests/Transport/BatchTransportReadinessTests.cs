using Unityctl.Core.Discovery;
using Unityctl.Core.Platform;
using Unityctl.Core.Transport;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Core.Tests.Transport;

public sealed class BatchTransportReadinessTests : IDisposable
{
    private readonly string _projectPath;

    public BatchTransportReadinessTests()
    {
        _projectPath = Path.Combine(Path.GetTempPath(), $"unityctl-batch-readiness-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_projectPath);
    }

    [Fact]
    public async Task SendAsync_LockedByInteractiveEditor_ReturnsProjectLockedGuidance()
    {
        var platform = new FakePlatform(
            locked: true,
            new UnityProcessInfo
            {
                ProcessId = 42,
                ProjectPath = _projectPath,
                HasMainWindow = true
            });
        var transport = CreateTransport(platform);

        var response = await transport.SendAsync(new CommandRequest { Command = WellKnownCommands.Status });

        Assert.False(response.Success);
        Assert.Equal(StatusCode.ProjectLocked, response.StatusCode);
        Assert.Contains("status --wait", response.Message);
    }

    [Fact]
    public async Task SendAsync_LockedByHeadlessProcess_ReturnsBusyGuidance()
    {
        var platform = new FakePlatform(
            locked: true,
            new UnityProcessInfo
            {
                ProcessId = 99,
                ProjectPath = _projectPath,
                IsBatchMode = true
            });
        var transport = CreateTransport(platform);

        var response = await transport.SendAsync(new CommandRequest { Command = WellKnownCommands.ProjectValidate });

        Assert.False(response.Success);
        Assert.Equal(StatusCode.Busy, response.StatusCode);
        Assert.Contains("headless Unity process", response.Message);
        Assert.Contains("99", response.Message);
    }

    [Fact]
    public async Task SendAsync_LockedWithoutMatchingProcess_ReturnsStaleLockGuidance()
    {
        var transport = CreateTransport(new FakePlatform(locked: true));

        var response = await transport.SendAsync(new CommandRequest { Command = WellKnownCommands.Check });

        Assert.False(response.Success);
        Assert.Equal(StatusCode.ProjectLocked, response.StatusCode);
        Assert.Contains("stale lock", response.Message);
    }

    public void Dispose()
    {
        if (Directory.Exists(_projectPath))
            Directory.Delete(_projectPath, recursive: true);
    }

    private BatchTransport CreateTransport(FakePlatform platform)
        => new(platform, new UnityEditorDiscovery(platform), _projectPath);

    private sealed class FakePlatform : IPlatformServices
    {
        private readonly bool _locked;
        private readonly IReadOnlyList<UnityProcessInfo> _processes;

        public FakePlatform(bool locked, params UnityProcessInfo[] processes)
        {
            _locked = locked;
            _processes = processes;
        }

        public string GetUnityHubEditorsJsonPath() => Path.Combine(Path.GetTempPath(), "missing-editors.json");

        public IEnumerable<string> GetDefaultEditorSearchPaths() => [];

        public string GetUnityExecutablePath(string editorBasePath) => Path.Combine(editorBasePath, "Unity.exe");

        public IEnumerable<UnityProcessInfo> FindRunningUnityProcesses() => _processes;

        public bool IsProjectLocked(string projectPath) => _locked;

        public Stream CreateIpcClientStream(string projectPath) => throw new NotSupportedException();

        public string GetTempResponseFilePath()
            => Path.Combine(Path.GetTempPath(), $"unityctl-batch-readiness-{Guid.NewGuid():N}.json");
    }
}
