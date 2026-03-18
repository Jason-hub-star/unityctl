using Unityctl.Core.Diagnostics;
using Xunit;

namespace Unityctl.Core.Tests.Diagnostics;

public class EditorLogDiagnosticsTests : IDisposable
{
    private readonly string _tempDir;

    public EditorLogDiagnosticsTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"unityctl-diag-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    private string WriteTempLog(string content)
    {
        var path = Path.Combine(_tempDir, "Editor.log");
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void GetEditorLogPath_ReturnsNonNull()
    {
        var path = EditorLogDiagnostics.GetEditorLogPath();
        Assert.NotNull(path);
        Assert.Contains("Editor.log", path);
    }

    [Fact]
    public void GetRecentDiagnostics_NullPath_ReturnsNull()
    {
        var result = EditorLogDiagnostics.GetRecentDiagnostics(logPath: null);
        Assert.Null(result);
    }

    [Fact]
    public void GetRecentDiagnostics_NonExistentFile_ReturnsNull()
    {
        var result = EditorLogDiagnostics.GetRecentDiagnostics(
            logPath: Path.Combine(_tempDir, "does-not-exist.log"));
        Assert.Null(result);
    }

    [Fact]
    public void GetRecentDiagnostics_NoErrors_ReturnsNull()
    {
        var logPath = WriteTempLog("""
            [unityctl] IPC server started on pipe: unityctl_abc123
            Refreshing native plugins compatible for Editor in 3.14 ms
            All good here
            """);

        var result = EditorLogDiagnostics.GetRecentDiagnostics(logPath);
        Assert.Null(result);
    }

    [Fact]
    public void GetRecentDiagnostics_WithCompileErrors_ReturnsDiagnostics()
    {
        var logPath = WriteTempLog("""
            Starting compilation...
            Assets/Scripts/Foo.cs(10,5): error CS1002: ; expected
            Assets/Scripts/Bar.cs(20,3): error CS0246: The type or namespace name 'Xyz' could not be found
            Compilation finished with errors
            """);

        var result = EditorLogDiagnostics.GetRecentDiagnostics(logPath);
        Assert.NotNull(result);
        Assert.Contains("error CS1002", result);
        Assert.Contains("error CS0246", result);
        Assert.Contains("Editor.log diagnostics", result);
        Assert.Contains("Log:", result);
    }

    [Fact]
    public void GetRecentDiagnostics_WithAsmdefErrors_ReturnsDiagnostics()
    {
        var logPath = WriteTempLog("""
            Loading packages...
            Required property 'name' not found in JSON. Path '', line 1, position 2. (Packages/com.test/test.asmdef)
            Done loading
            """);

        var result = EditorLogDiagnostics.GetRecentDiagnostics(logPath);
        Assert.NotNull(result);
        Assert.Contains("Required property", result);
        Assert.Contains(".asmdef", result);
    }

    [Fact]
    public void GetRecentDiagnostics_IncludesUnityctlLines_WhenErrorsPresent()
    {
        var logPath = WriteTempLog("""
            [unityctl] IPC server started on pipe: unityctl_abc123
            Assets/Scripts/Foo.cs(10,5): error CS1002: ; expected
            [unityctl] Command handler registered: status
            """);

        var result = EditorLogDiagnostics.GetRecentDiagnostics(logPath);
        Assert.NotNull(result);
        Assert.Contains("error CS1002", result);
        Assert.Contains("[unityctl]", result);
    }

    [Fact]
    public void GetRecentDiagnostics_LimitsCompileErrors_ToMax5()
    {
        var lines = new List<string>();
        for (var i = 0; i < 10; i++)
            lines.Add($"Assets/Scripts/File{i}.cs({i},1): error CS{1000 + i}: Error {i}");

        var logPath = WriteTempLog(string.Join(Environment.NewLine, lines));

        var result = EditorLogDiagnostics.GetRecentDiagnostics(logPath);
        Assert.NotNull(result);

        // Should contain first 5 errors only
        for (var i = 0; i < 5; i++)
            Assert.Contains($"error CS{1000 + i}", result);
        for (var i = 5; i < 10; i++)
            Assert.DoesNotContain($"error CS{1000 + i}", result);
    }

    [Fact]
    public void GetRecentDiagnostics_RespectseTailLines()
    {
        var lines = new List<string>();
        // Old error outside tail window
        lines.Add("Assets/Old.cs(1,1): error CS9999: Old error");
        for (var i = 0; i < 50; i++)
            lines.Add($"Normal log line {i}");

        var logPath = WriteTempLog(string.Join(Environment.NewLine, lines));

        // tailLines=10 should not reach the old error at the top
        var result = EditorLogDiagnostics.GetRecentDiagnostics(logPath, tailLines: 10);
        Assert.Null(result);
    }

    [Fact]
    public void GetStructuredDiagnostics_NullPath_ReturnsNull()
    {
        var result = EditorLogDiagnostics.GetStructuredDiagnostics(logPath: null);
        Assert.Null(result);
    }

    [Fact]
    public void GetStructuredDiagnostics_WithErrors_ReturnsSeparatedLists()
    {
        var logPath = WriteTempLog("""
            [unityctl] IPC server started
            Assets/Scripts/Foo.cs(10,5): error CS1002: ; expected
            [unityctl] Command registered
            """);

        var result = EditorLogDiagnostics.GetStructuredDiagnostics(logPath);
        Assert.NotNull(result);
        Assert.Single(result.Value.Errors);
        Assert.Contains("error CS1002", result.Value.Errors[0]);
        Assert.Equal(2, result.Value.UnityctlLines.Count);
    }

    [Fact]
    public void GetStructuredDiagnostics_NoErrors_NoUnityctlLines_ReturnsNull()
    {
        var logPath = WriteTempLog("""
            Normal log line 1
            Normal log line 2
            """);

        var result = EditorLogDiagnostics.GetStructuredDiagnostics(logPath);
        Assert.Null(result);
    }
}
