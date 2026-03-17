using System.Text.Json;
using Unityctl.Shared.Protocol;
using Unityctl.Shared.Serialization;
using Xunit;

namespace Unityctl.Shared.Tests;

public class ProtocolTests
{
    [Fact]
    public void CommandRequest_RoundTrip()
    {
        var request = new CommandRequest
        {
            Command = "build",
            Parameters = new Dictionary<string, object?> { ["target"] = "StandaloneWindows64" }
        };

        var json = JsonSerializer.Serialize(request, UnityctlJsonContext.Default.CommandRequest);
        var deserialized = JsonSerializer.Deserialize(json, UnityctlJsonContext.Default.CommandRequest);

        Assert.NotNull(deserialized);
        Assert.Equal("build", deserialized!.Command);
        Assert.NotNull(deserialized.RequestId);
    }

    [Fact]
    public void CommandResponse_Ok_HasCorrectStatusCode()
    {
        var response = CommandResponse.Ok("done");

        Assert.True(response.Success);
        Assert.Equal(StatusCode.Ready, response.StatusCode);
        Assert.Equal("done", response.Message);
    }

    [Fact]
    public void CommandResponse_Fail_HasCorrectStatusCode()
    {
        var response = CommandResponse.Fail(StatusCode.BuildFailed, "build error",
            new List<string> { "error1" });

        Assert.False(response.Success);
        Assert.Equal(StatusCode.BuildFailed, response.StatusCode);
        Assert.Single(response.Errors!);
    }

    [Fact]
    public void CommandResponse_RoundTrip()
    {
        var response = CommandResponse.Ok("test", new Dictionary<string, object?> { ["key"] = "value" });
        var json = JsonSerializer.Serialize(response, UnityctlJsonContext.Default.CommandResponse);
        var deserialized = JsonSerializer.Deserialize(json, UnityctlJsonContext.Default.CommandResponse);

        Assert.NotNull(deserialized);
        Assert.True(deserialized!.Success);
        Assert.Equal("test", deserialized.Message);
    }

    [Theory]
    [InlineData(StatusCode.Compiling, true)]
    [InlineData(StatusCode.Busy, true)]
    [InlineData(StatusCode.NotFound, false)]
    [InlineData(StatusCode.BuildFailed, false)]
    [InlineData(StatusCode.Ready, false)]
    public void StatusCode_IsTransient(StatusCode code, bool expectedTransient)
    {
        var isTransient = (int)code >= 100 && (int)code < 200;
        Assert.Equal(expectedTransient, isTransient);
    }
}
