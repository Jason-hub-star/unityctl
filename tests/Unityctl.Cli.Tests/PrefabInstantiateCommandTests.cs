using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class PrefabInstantiateCommandTests
{
    [Fact]
    public void InstantiateRequest_HasCorrectCommand()
    {
        var request = PrefabCommand.CreateInstantiateRequest("Assets/Prefabs/Enemy.prefab");
        Assert.Equal(WellKnownCommands.PrefabInstantiate, request.Command);
    }

    [Fact]
    public void InstantiateRequest_SetsPath()
    {
        var request = PrefabCommand.CreateInstantiateRequest("Assets/Prefabs/Enemy.prefab");
        Assert.Equal("Assets/Prefabs/Enemy.prefab", request.Parameters!["path"]?.GetValue<string>());
    }

    [Fact]
    public void InstantiateRequest_SetsOptionalParameters()
    {
        var request = PrefabCommand.CreateInstantiateRequest(
            "Assets/Prefabs/Enemy.prefab",
            name: "Enemy_1",
            parent: "gid://some-id",
            position: "[1,2,3]",
            rotation: "[0,90,0]",
            scale: "[2,2,2]");

        Assert.Equal("Enemy_1", request.Parameters!["name"]?.GetValue<string>());
        Assert.Equal("gid://some-id", request.Parameters!["parent"]?.GetValue<string>());
        Assert.Equal("[1,2,3]", request.Parameters!["position"]?.GetValue<string>());
        Assert.Equal("[0,90,0]", request.Parameters!["rotation"]?.GetValue<string>());
        Assert.Equal("[2,2,2]", request.Parameters!["scale"]?.GetValue<string>());
    }

    [Fact]
    public void InstantiateRequest_OmitsNullOptionals()
    {
        var request = PrefabCommand.CreateInstantiateRequest("Assets/Prefabs/Enemy.prefab", name: "Test");
        Assert.Equal("Test", request.Parameters!["name"]?.GetValue<string>());
        Assert.Null(request.Parameters!["parent"]);
        Assert.Null(request.Parameters!["position"]);
    }

    [Fact]
    public void InstantiateRequest_ThrowsOnEmptyPath()
    {
        Assert.Throws<ArgumentException>(() => PrefabCommand.CreateInstantiateRequest(""));
    }
}
