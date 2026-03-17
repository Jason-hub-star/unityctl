using Unityctl.Core.Platform;
using Unityctl.Core.Discovery;
using Xunit;

namespace Unityctl.Cli.Tests;

public class UnityEditorDiscoveryTests
{
    [Theory]
    [InlineData("m_EditorVersion: 2021.3.11f1\nm_EditorVersionWithRevision: 2021.3.11f1 (abc123)", "2021.3.11f1")]
    [InlineData("m_EditorVersion: 6000.0.64f1\n", "6000.0.64f1")]
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

    private static void CreateEditor(string root, string version)
    {
        var editorDirectory = Path.Combine(root, version);
        Directory.CreateDirectory(editorDirectory);
        File.WriteAllText(Path.Combine(editorDirectory, "Unity.exe"), string.Empty);
    }

    private sealed class FakePlatform : IPlatformServices
    {
        private readonly string _editorsRoot;

        public FakePlatform(string editorsRoot)
        {
            _editorsRoot = editorsRoot;
        }

        public string GetUnityHubEditorsJsonPath() => Path.Combine(_editorsRoot, "editors.json");

        public IEnumerable<string> GetDefaultEditorSearchPaths()
        {
            yield return _editorsRoot;
        }

        public string GetUnityExecutablePath(string editorBasePath)
            => Path.Combine(editorBasePath, "Unity.exe");

        public IEnumerable<UnityProcessInfo> FindRunningUnityProcesses()
            => [];

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
