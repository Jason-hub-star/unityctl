using System.Runtime.InteropServices;
using Unityctl.Core.Platform;
using Unityctl.Core.Discovery;
using Unityctl.Shared;
using Xunit;

namespace Unityctl.Cli.Tests;

public class UnityEditorDiscoveryTests
{
    [Theory]
    [InlineData("m_EditorVersion: 2021.3.11f1\nm_EditorVersionWithRevision: 2021.3.11f1 (abc123)", "2021.3.11f1")]
    [InlineData("m_EditorVersion: 6000.0.64f1\n", "6000.0.64f1")]
    [InlineData("  m_EditorVersion: 2022.3.20f1\r\nm_EditorVersionWithRevision: ignored", "2022.3.20f1")]
    [InlineData("nothing here", null)]
    [InlineData("", null)]
    public void ParseProjectVersion_ExtractsVersion(string content, string? expected)
    {
        var result = UnityEditorDiscovery.ParseProjectVersion(content);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FindEditors_SortsVersionsNumerically()
    {
        using var tempDirectory = new TemporaryDirectory();
        var editorsRoot = Path.Combine(tempDirectory.Path, "editors");
        CreateEditor(editorsRoot, "2022.3.0f1");
        CreateEditor(editorsRoot, "2022.10.0f1");

        var discovery = new UnityEditorDiscovery(new FakePlatform(editorsRoot));

        var editors = discovery.FindEditors();

        Assert.Collection(
            editors,
            editor => Assert.Equal("2022.10.0f1", editor.Version),
            editor => Assert.Equal("2022.3.0f1", editor.Version));
    }

    [Fact]
    public void FindEditors_ReadsUnityHubEditorsJson_LocationCaseInsensitive()
    {
        using var tempDirectory = new TemporaryDirectory();
        var editorsRoot = Path.Combine(tempDirectory.Path, "editors");
        var editorDirectory = CreateEditor(editorsRoot, "6000.0.64f1");
        Directory.CreateDirectory(editorsRoot);
        File.WriteAllText(
            Path.Combine(editorsRoot, "editors.json"),
            $$"""
            {
              "6000.0.64f1": {
                "Location": "{{editorDirectory.Replace("\\", "\\\\")}}"
              }
            }
            """);

        var discovery = new UnityEditorDiscovery(new FakePlatform(editorsRoot));

        var editor = Assert.Single(discovery.FindEditors());
        Assert.Equal("6000.0.64f1", editor.Version);
        Assert.Equal(editorDirectory, editor.Location);
    }

    [Fact]
    public void FindEditorForProject_FallsBackToNewestMatchingMajorVersion()
    {
        using var tempDirectory = new TemporaryDirectory();
        var editorsRoot = Path.Combine(tempDirectory.Path, "editors");
        CreateEditor(editorsRoot, "2022.3.0f1");
        CreateEditor(editorsRoot, "2022.10.0f1");

        var projectPath = Path.Combine(tempDirectory.Path, "MyProject");
        Directory.CreateDirectory(Path.Combine(projectPath, "ProjectSettings"));
        File.WriteAllText(
            Path.Combine(projectPath, "ProjectSettings", "ProjectVersion.txt"),
            "m_EditorVersion: 2022.1.0f1");

        var discovery = new UnityEditorDiscovery(new FakePlatform(editorsRoot));

        var editor = discovery.FindEditorForProject(projectPath);

        Assert.NotNull(editor);
        Assert.Equal("2022.10.0f1", editor!.Version);
    }

    [Fact]
    public void FindRunningEditorInstances_ClassifiesInteractiveAndHeadlessProcesses()
    {
        using var tempDirectory = new TemporaryDirectory();
        var editorsRoot = Path.Combine(tempDirectory.Path, "editors");
        var editorDirectory = CreateEditor(editorsRoot, "2022.3.0f1");
        var projectPath = Path.Combine(tempDirectory.Path, "MyProject");
        var executablePath = Path.Combine(editorDirectory, "Unity.exe");
        var platform = new FakePlatform(
            editorsRoot,
            new[]
            {
                new UnityProcessInfo
                {
                    ProcessId = 100,
                    ProjectPath = projectPath,
                    Version = "2022.3.0f1",
                    ExecutablePath = executablePath,
                    HasMainWindow = true
                },
                new UnityProcessInfo
                {
                    ProcessId = 101,
                    ProjectPath = projectPath,
                    Version = "2022.3.0f1",
                    ExecutablePath = executablePath,
                    IsBatchMode = true
                }
            });

        var discovery = new UnityEditorDiscovery(platform);

        var instances = discovery.FindRunningEditorInstances(probeIpc: false);

        Assert.Collection(
            instances,
            interactive => Assert.Equal("interactive", interactive.ProcessKind),
            headless => Assert.Equal("headless", headless.ProcessKind));
        Assert.All(instances, instance => Assert.NotNull(instance.PipeName));
    }

    [Fact]
    public void FindEditors_RunningProjectPathCaseBehaviorFollowsPlatformPolicy()
    {
        using var tempDirectory = new TemporaryDirectory();
        var editorsRoot = Path.Combine(tempDirectory.Path, "editors");
        var editorDirectory = CreateEditor(editorsRoot, "6000.0.64f1");
        var executablePath = Path.Combine(editorDirectory, "Unity.exe");
        var lowerProjectPath = Path.Combine(tempDirectory.Path, "case-project");
        var upperProjectPath = Path.Combine(tempDirectory.Path, "CASE-PROJECT");
        var platform = new FakePlatform(
            editorsRoot,
            new[]
            {
                new UnityProcessInfo
                {
                    ProcessId = 100,
                    ProjectPath = lowerProjectPath,
                    Version = "6000.0.64f1",
                    ExecutablePath = executablePath,
                    HasMainWindow = true
                },
                new UnityProcessInfo
                {
                    ProcessId = 101,
                    ProjectPath = upperProjectPath,
                    Version = "6000.0.64f1",
                    ExecutablePath = executablePath,
                    HasMainWindow = true
                }
            });

        var discovery = new UnityEditorDiscovery(platform);

        var editor = Assert.Single(discovery.FindEditors());
        Assert.NotNull(editor.RunningProjectPaths);
        var runningProjectPaths = editor.RunningProjectPaths;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.Single(runningProjectPaths);
            Assert.Equal(Constants.NormalizeProjectPath(lowerProjectPath), runningProjectPaths[0]);
        }
        else
        {
            Assert.Equal(
                new[]
                {
                    Constants.NormalizeProjectPath(upperProjectPath),
                    Constants.NormalizeProjectPath(lowerProjectPath)
                },
                runningProjectPaths);
        }
    }

    private static string CreateEditor(string root, string version)
    {
        var editorDirectory = Path.Combine(root, version);
        Directory.CreateDirectory(editorDirectory);
        File.WriteAllText(Path.Combine(editorDirectory, "Unity.exe"), string.Empty);
        return editorDirectory;
    }

    private sealed class FakePlatform : IPlatformServices
    {
        private readonly string _editorsRoot;
        private readonly IReadOnlyList<UnityProcessInfo> _processes;

        public FakePlatform(string editorsRoot, IReadOnlyList<UnityProcessInfo>? processes = null)
        {
            _editorsRoot = editorsRoot;
            _processes = processes ?? [];
        }

        public string GetUnityHubEditorsJsonPath() => Path.Combine(_editorsRoot, "editors.json");

        public IEnumerable<string> GetDefaultEditorSearchPaths()
        {
            yield return _editorsRoot;
        }

        public string GetUnityExecutablePath(string editorBasePath)
            => Path.Combine(editorBasePath, "Unity.exe");

        public IEnumerable<UnityProcessInfo> FindRunningUnityProcesses()
            => _processes;

        public bool IsProjectLocked(string projectPath) => false;

        public Stream CreateIpcClientStream(string projectPath)
            => throw new NotSupportedException();

        public string GetTempResponseFilePath()
            => Path.Combine(Path.GetTempPath(), $"unityctl-cli-test-{Guid.NewGuid():N}.json");
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"unityctl-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
