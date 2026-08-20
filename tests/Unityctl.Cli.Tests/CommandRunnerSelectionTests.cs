using Unityctl.Cli.Execution;
using Unityctl.Core.EditorRouting;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public sealed class CommandRunnerSelectionTests : IDisposable
{
    private readonly string _configDir;
    private readonly EditorSelectionStore _store;

    public CommandRunnerSelectionTests()
    {
        _configDir = Path.Combine(Path.GetTempPath(), $"unityctl-cli-selection-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_configDir);
        _store = new EditorSelectionStore(_configDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_configDir, recursive: true); } catch { }
    }

    [CliTestFact]
    public void TryResolveProject_WithExplicitProject_ReturnsFullPath()
    {
        var projectPath = Path.Combine(_configDir, "ProjectA");
        Directory.CreateDirectory(Path.Combine(projectPath, "ProjectSettings"));
        File.WriteAllText(Path.Combine(projectPath, "ProjectSettings", "ProjectVersion.txt"), "m_EditorVersion: 6000.0.64f1");

        var ok = CommandRunner.TryResolveProject(projectPath, out var resolvedProject, out var failureResponse, _store);

        Assert.True(ok);
        Assert.Equal(Path.GetFullPath(projectPath), resolvedProject);
        Assert.Null(failureResponse);
    }

    [CliTestFact]
    public void TryResolveProject_ExplicitProjectMissing_FailsWithPathMessage()
    {
        var missing = Path.Combine(_configDir, "NoSuchProject");

        var ok = CommandRunner.TryResolveProject(missing, out var resolvedProject, out var failureResponse, _store);

        Assert.False(ok);
        Assert.Equal(string.Empty, resolvedProject);
        Assert.NotNull(failureResponse);
        Assert.Equal(StatusCode.InvalidParameters, failureResponse!.StatusCode);
        Assert.Contains("does not exist", failureResponse.Message);
    }

    [CliTestFact]
    public void TryResolveProject_ExplicitProjectNotUnity_FailsWithProjectVersionMessage()
    {
        var notUnity = Path.Combine(_configDir, "PlainFolder");
        Directory.CreateDirectory(notUnity);

        var ok = CommandRunner.TryResolveProject(notUnity, out var resolvedProject, out var failureResponse, _store);

        Assert.False(ok);
        Assert.Equal(string.Empty, resolvedProject);
        Assert.NotNull(failureResponse);
        Assert.Equal(StatusCode.InvalidParameters, failureResponse!.StatusCode);
        Assert.Contains("ProjectVersion.txt", failureResponse.Message);
    }

    [CliTestFact]
    public void TryResolveProject_WithoutSelection_ReturnsFailure()
    {
        var ok = CommandRunner.TryResolveProject(null, out var resolvedProject, out var failureResponse, _store);

        Assert.False(ok);
        Assert.Equal(string.Empty, resolvedProject);
        Assert.NotNull(failureResponse);
        Assert.Equal(StatusCode.InvalidParameters, failureResponse!.StatusCode);
    }

    [CliTestFact]
    public void TryResolveProject_StaleSelection_FallsBackToCwdProject()
    {
        var stalePath = Path.Combine(_configDir, "DeletedProject");
        _store.SaveProject(stalePath);

        var cwdProject = Path.Combine(_configDir, "CwdProject");
        Directory.CreateDirectory(Path.Combine(cwdProject, "ProjectSettings"));
        File.WriteAllText(Path.Combine(cwdProject, "ProjectSettings", "ProjectVersion.txt"), "m_EditorVersion: 6000.0.64f1");
        Directory.CreateDirectory(Path.Combine(cwdProject, "Assets"));

        var ok = CommandRunner.TryResolveProject(null, out var resolvedProject, out var failureResponse, _store, cwdProject);

        Assert.True(ok);
        Assert.Equal(Path.GetFullPath(cwdProject), resolvedProject);
        Assert.Null(failureResponse);
    }

    [CliTestFact]
    public void TryResolveProject_StaleSelection_WithoutCwdProject_FailsWithGuidance()
    {
        var stalePath = Path.Combine(_configDir, "DeletedProject");
        _store.SaveProject(stalePath);

        var nonProjectDir = Path.Combine(_configDir, "NotAProject");
        Directory.CreateDirectory(nonProjectDir);

        var ok = CommandRunner.TryResolveProject(null, out var resolvedProject, out var failureResponse, _store, nonProjectDir);

        Assert.False(ok);
        Assert.Equal(string.Empty, resolvedProject);
        Assert.NotNull(failureResponse);
        Assert.Equal(StatusCode.InvalidParameters, failureResponse!.StatusCode);
        Assert.Contains("stale", failureResponse.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--project", failureResponse.Message);
    }

    [CliTestFact]
    public void TryResolveProject_UsesSelectedProject()
    {
        var projectPath = Path.Combine(_configDir, "SelectedProject");
        Directory.CreateDirectory(Path.Combine(projectPath, "ProjectSettings"));
        File.WriteAllText(Path.Combine(projectPath, "ProjectSettings", "ProjectVersion.txt"), "m_EditorVersion: 6000.0.64f1");

        _store.SaveProject(projectPath);

        var ok = CommandRunner.TryResolveProject(null, out var resolvedProject, out var failureResponse, _store);

        Assert.True(ok);
        Assert.Equal(Unityctl.Shared.Constants.NormalizeProjectPath(projectPath), resolvedProject);
        Assert.Null(failureResponse);
    }
}
