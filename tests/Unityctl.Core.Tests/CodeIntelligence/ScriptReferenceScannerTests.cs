using System.Text.Json.Nodes;
using Unityctl.Core.CodeIntelligence;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Core.Tests.CodeIntelligence;

public sealed class ScriptReferenceScannerTests : IDisposable
{
    private readonly string _project =
        Path.Combine(Path.GetTempPath(), $"unityctl-reference-scan-{Guid.NewGuid():N}");

    public ScriptReferenceScannerTests() =>
        Directory.CreateDirectory(Path.Combine(_project, "Assets"));

    [Fact]
    public void Execute_FindsEveryWordBoundaryWithoutAnEditor()
    {
        File.WriteAllText(
            Path.Combine(_project, "Assets", "Probe.cs"),
            "var pair = target.Value + target.Value;\nvar targetExtra = 0;\n");
        var request = Request("target");

        var response = ScriptReferenceScanner.Execute(_project, request);

        Assert.True(response.Success);
        Assert.Equal(request.RequestId, response.RequestId);
        Assert.Equal(2, response.Data!["referenceCount"]!.GetValue<int>());
        Assert.False(response.Data["truncated"]!.GetValue<bool>());
        Assert.Equal("local", response.Data["target"]!["transport"]!.GetValue<string>());
        var references = response.Data["references"]!.AsArray();
        Assert.Equal(12, references[0]!["column"]!.GetValue<int>());
        Assert.Equal(27, references[1]!["column"]!.GetValue<int>());
    }

    [Fact]
    public void Execute_ReportsTruncationOnlyWhenAnotherMatchExists()
    {
        File.WriteAllText(
            Path.Combine(_project, "Assets", "Probe.cs"),
            "target target target\n");
        var request = Request("target");
        request.Parameters!["limit"] = 2;

        var response = ScriptReferenceScanner.Execute(_project, request);

        Assert.Equal(2, response.Data!["referenceCount"]!.GetValue<int>());
        Assert.True(response.Data["truncated"]!.GetValue<bool>());
    }

    [Fact]
    public void Execute_RejectsFoldersOutsideProject()
    {
        var request = Request("target");
        request.Parameters!["folder"] = "..";

        var response = ScriptReferenceScanner.Execute(_project, request);

        Assert.False(response.Success);
        Assert.Equal(StatusCode.InvalidParameters, response.StatusCode);
    }

    public void Dispose() => Directory.Delete(_project, recursive: true);

    private static CommandRequest Request(string symbol) =>
        new()
        {
            Command = "script-find-refs",
            Parameters = new JsonObject { ["symbol"] = symbol }
        };
}
