using System.Text.Json;
using System.Text.Json.Nodes;
using Unityctl.Shared;
using Unityctl.Shared.Protocol;
using Unityctl.Shared.Serialization;
using Xunit;

namespace Unityctl.Shared.Tests;

public class ProtocolTests
{
    [Fact]
    public void CommandRequest_RoundTrip_JsonObject()
    {
        var request = new CommandRequest
        {
            Command = "build",
            Parameters = new JsonObject { ["target"] = "StandaloneWindows64" }
        };

        var json = JsonSerializer.Serialize(request, UnityctlJsonContext.Default.CommandRequest);
        var deserialized = JsonSerializer.Deserialize(json, UnityctlJsonContext.Default.CommandRequest);

        Assert.NotNull(deserialized);
        Assert.Equal("build", deserialized!.Command);
        Assert.NotNull(deserialized.RequestId);
        Assert.Equal("StandaloneWindows64", deserialized.GetParam("target"));
    }

    [Fact]
    public void CommandRequest_GetParam_ReturnsDefault()
    {
        var request = new CommandRequest { Command = "test" };
        Assert.Null(request.GetParam("missing"));
        Assert.Equal("fallback", request.GetParam("missing", "fallback"));
    }

    [Fact]
    public void CommandRequest_GetParam_WithNested()
    {
        var request = new CommandRequest
        {
            Command = "build",
            Parameters = new JsonObject
            {
                ["target"] = "Win64",
                ["nested"] = new JsonObject { ["a"] = 1 }
            }
        };

        Assert.Equal("Win64", request.GetParam("target"));
        Assert.Null(request.GetParam("nonexistent"));
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
    public void CommandResponse_Ok_WithJsonObject()
    {
        var data = new JsonObject { ["key"] = "value", ["count"] = 42 };
        var response = CommandResponse.Ok("test", data);

        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal("value", response.Data!["key"]?.GetValue<string>());
        Assert.Equal(42, response.Data["count"]?.GetValue<int>());
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
    public void CommandResponse_RoundTrip_JsonObject()
    {
        var data = new JsonObject { ["key"] = "value" };
        var response = CommandResponse.Ok("test", data);
        var json = JsonSerializer.Serialize(response, UnityctlJsonContext.Default.CommandResponse);
        var deserialized = JsonSerializer.Deserialize(json, UnityctlJsonContext.Default.CommandResponse);

        Assert.NotNull(deserialized);
        Assert.True(deserialized!.Success);
        Assert.Equal("test", deserialized.Message);
        Assert.NotNull(deserialized.Data);
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

    [Fact]
    public void CommandRequest_GetParamT_Int()
    {
        var request = new CommandRequest
        {
            Command = "build",
            Parameters = new JsonObject { ["count"] = 42, ["flag"] = true }
        };

        Assert.Equal(42, request.GetParam<int>("count"));
        Assert.True(request.GetParam<bool>("flag"));
        Assert.Equal(0, request.GetParam<int>("missing"));
        Assert.Equal(99, request.GetParam<int>("missing", 99));
    }

    [Fact]
    public void CommandRequest_GetObjectParam()
    {
        var request = new CommandRequest
        {
            Command = "build",
            Parameters = new JsonObject
            {
                ["options"] = new JsonObject { ["a"] = 1, ["b"] = "two" },
                ["simple"] = "text"
            }
        };

        var obj = request.GetObjectParam("options");
        Assert.NotNull(obj);
        Assert.Equal(1, obj!["a"]?.GetValue<int>());
        Assert.Equal("two", obj["b"]?.GetValue<string>());

        Assert.Null(request.GetObjectParam("simple"));
        Assert.Null(request.GetObjectParam("nonexistent"));
    }

    [Fact]
    public void CommandRequest_GetParamT_NullParameters()
    {
        var request = new CommandRequest { Command = "test" };
        Assert.Equal(0, request.GetParam<int>("x"));
        Assert.False(request.GetParam<bool>("y"));
        Assert.Null(request.GetObjectParam("z"));
    }

    [Fact]
    public void CommandRequest_RoundTrip_WithCommandArray()
    {
        var request = new CommandRequest
        {
            Command = "batch-execute",
            Parameters = new JsonObject
            {
                ["rollbackOnFailure"] = true,
                ["commands"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["command"] = "gameobject-create",
                        ["parameters"] = new JsonObject { ["name"] = "Cube A" }
                    },
                    new JsonObject
                    {
                        ["command"] = "gameobject-create",
                        ["parameters"] = new JsonObject { ["name"] = "Cube B" }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(request, UnityctlJsonContext.Default.CommandRequest);
        var deserialized = JsonSerializer.Deserialize(json, UnityctlJsonContext.Default.CommandRequest);

        Assert.NotNull(deserialized);
        Assert.True(deserialized!.GetParam("rollbackOnFailure", false));

        var commands = deserialized.Parameters!["commands"]?.AsArray();
        Assert.NotNull(commands);
        Assert.Equal(2, commands!.Count);
        Assert.Equal("gameobject-create", commands[0]!["command"]?.GetValue<string>());
        Assert.Equal("Cube B", commands[1]!["parameters"]!["name"]?.GetValue<string>());
    }

    [Fact]
    public void EventEnvelope_RoundTrip()
    {
        var envelope = new EventEnvelope
        {
            Channel = "console",
            EventType = "log",
            Timestamp = 1742208000000,
            Payload = new JsonObject { ["message"] = "Hello" }
        };

        var json = JsonSerializer.Serialize(envelope, UnityctlJsonContext.Default.EventEnvelope);
        var deserialized = JsonSerializer.Deserialize(json, UnityctlJsonContext.Default.EventEnvelope);

        Assert.NotNull(deserialized);
        Assert.Equal("console", deserialized!.Channel);
        Assert.Equal("log", deserialized.EventType);
        Assert.Equal("Hello", deserialized.Payload?["message"]?.GetValue<string>());
    }
}
