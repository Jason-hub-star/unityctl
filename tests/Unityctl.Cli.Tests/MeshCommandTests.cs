using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class MeshCommandTests
{
    [Fact]
    public void CreatePrimitiveRequest_HasCorrectCommand()
    {
        var request = MeshCommand.CreatePrimitiveRequest("Cube");
        Assert.Equal(WellKnownCommands.MeshCreatePrimitive, request.Command);
    }

    [Fact]
    public void CreatePrimitiveRequest_SetsType()
    {
        var request = MeshCommand.CreatePrimitiveRequest("Sphere");
        Assert.Equal("Sphere", request.Parameters!["type"]!.ToString());
    }

    [Fact]
    public void CreatePrimitiveRequest_EmptyType_Throws()
    {
        Assert.Throws<ArgumentException>(() => MeshCommand.CreatePrimitiveRequest(""));
    }

    [Fact]
    public void CreatePrimitiveRequest_SetsName()
    {
        var request = MeshCommand.CreatePrimitiveRequest("Cube", name: "Wall");
        Assert.Equal("Wall", request.Parameters!["name"]!.ToString());
    }

    [Fact]
    public void CreatePrimitiveRequest_SetsPosition()
    {
        var request = MeshCommand.CreatePrimitiveRequest("Cube", position: "[1,2,3]");
        Assert.Equal("[1,2,3]", request.Parameters!["position"]!.ToString());
    }

    [Fact]
    public void CreatePrimitiveRequest_SetsScale()
    {
        var request = MeshCommand.CreatePrimitiveRequest("Plane", scale: "[10,1,10]");
        Assert.Equal("[10,1,10]", request.Parameters!["scale"]!.ToString());
    }

    [Fact]
    public void CreatePrimitiveRequest_SetsMaterial()
    {
        var request = MeshCommand.CreatePrimitiveRequest("Cube", material: "Assets/Mat.mat");
        Assert.Equal("Assets/Mat.mat", request.Parameters!["material"]!.ToString());
    }

    [Fact]
    public void CreatePrimitiveRequest_NoOptional_OmitsKeys()
    {
        var request = MeshCommand.CreatePrimitiveRequest("Cube");
        Assert.Null(request.Parameters!["name"]);
        Assert.Null(request.Parameters!["position"]);
        Assert.Null(request.Parameters!["scale"]);
    }
}
