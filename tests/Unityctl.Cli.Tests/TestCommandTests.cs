using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class TestCommandTests
{
    [Fact]
    public void CreateResultRequest_HasCorrectCommand()
    {
        var request = TestCommand.CreateResultRequest("req-123");

        Assert.Equal(WellKnownCommands.TestResult, request.Command);
    }

    [Fact]
    public void CreateResultRequest_SetsRequestIdParameter()
    {
        var request = TestCommand.CreateResultRequest("req-123");

        Assert.Equal("req-123", request.Parameters!["requestId"]!.GetValue<string>());
    }

    [Fact]
    public void CreateResultRequest_EmptyRequestId_Throws()
    {
        Assert.Throws<ArgumentException>(() => TestCommand.CreateResultRequest(""));
    }
}
