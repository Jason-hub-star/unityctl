using System.Runtime.InteropServices;
using Unityctl.Core.Discovery;
using Unityctl.Core.Platform;
using Xunit;

namespace Unityctl.Core.Tests;

public sealed class UnityProcessDetectorTests
{
    [Fact]
    public void FindProcessesForProject_MatchesSlashVariantsButPreservesPlatformCasePolicy()
    {
        const string forwardSlashPath = "C:/Users/jason/My project";
        const string mixedSlashPath = @"C:\Users\jason\My project";
        const string caseOnlyPath = "C:/Users/jason/MY PROJECT";
        var detector = new UnityProcessDetector(new FakePlatform(
            new UnityProcessInfo { ProcessId = 100, ProjectPath = mixedSlashPath, HasMainWindow = true },
            new UnityProcessInfo { ProcessId = 101, ProjectPath = caseOnlyPath, HasMainWindow = true }));

        var processes = detector.FindProcessesForProject(forwardSlashPath);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.Equal(new[] { 100, 101 }, processes.Select(process => process.ProcessId).ToArray());
        }
        else
        {
            var process = Assert.Single(processes);
            Assert.Equal(100, process.ProcessId);
        }
    }

    private sealed class FakePlatform : IPlatformServices
    {
        private readonly IReadOnlyList<UnityProcessInfo> _processes;

        public FakePlatform(params UnityProcessInfo[] processes)
        {
            _processes = processes;
        }

        public string GetUnityHubEditorsJsonPath() => string.Empty;

        public IEnumerable<string> GetDefaultEditorSearchPaths() => Array.Empty<string>();

        public string GetUnityExecutablePath(string editorBasePath) => Path.Combine(editorBasePath, "Unity.exe");

        public IEnumerable<UnityProcessInfo> FindRunningUnityProcesses() => _processes;

        public bool IsProjectLocked(string projectPath) => false;

        public Stream CreateIpcClientStream(string projectPath) => throw new NotSupportedException();

        public string GetTempResponseFilePath()
            => Path.Combine(Path.GetTempPath(), $"unityctl-process-detector-{Guid.NewGuid():N}.json");
    }
}
