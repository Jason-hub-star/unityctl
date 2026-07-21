using Unityctl.Cli.Commands;
using Xunit;

namespace Unityctl.Cli.Tests;

public class RuntimeCommandTests
{
    [CliTestFact]
    public void CreateStatusRequest_SetsRuntimeStatusCommandName()
    {
        var request = RuntimeCommand.CreateStatusRequest();

        Assert.Equal("runtime-status", request.Command);
        Assert.False(string.IsNullOrEmpty(request.RequestId));
    }

    [CliTestFact]
    public void CreateLogsRequest_SetsLimitAndSeverity()
    {
        var request = RuntimeCommand.CreateLogsRequest(limit: 10, severity: "Error");

        Assert.Equal("runtime-logs", request.Command);
        Assert.Equal(10, request.Parameters!["limit"]?.GetValue<int>());
        Assert.Equal("Error", request.Parameters!["severity"]?.GetValue<string>());
    }

    [CliTestFact]
    public void CreateLogsRequest_OmitsUnsetParameters()
    {
        var request = RuntimeCommand.CreateLogsRequest();

        Assert.Null(request.Parameters!["limit"]);
        Assert.Null(request.Parameters!["severity"]);
    }

    [CliTestFact]
    public void ReadPipeName_ParsesStateFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"unityctl-runtime-test-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, "{\"pipeName\":\"unityctl_rt_1234\",\"pid\":1234}");
        try
        {
            Assert.Equal("unityctl_rt_1234", RuntimeCommand.ReadPipeName(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [CliTestFact]
    public void ReadPipeName_MissingFile_ReturnsNull()
    {
        Assert.Null(RuntimeCommand.ReadPipeName("/nonexistent/unityctl-runtime.json"));
    }

    [CliTestFact]
    public void ReadPipeName_MalformedJson_ReturnsNull()
    {
        var path = Path.Combine(Path.GetTempPath(), $"unityctl-runtime-test-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, "not json");
        try
        {
            Assert.Null(RuntimeCommand.ReadPipeName(path));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
