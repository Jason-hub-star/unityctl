#nullable enable

using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

/// <summary>
/// Tests for the TypeCommand request builder.
/// </summary>
public sealed class TypeCommandTests
{
    [Fact]
    public void CreateDescribeRequest_WithTypeName_SetsCommand()
    {
        var request = TypeCommand.CreateDescribeRequest("UnityEngine.Rigidbody");

        Assert.Equal(WellKnownCommands.DescribeType, request.Command);
    }

    [Fact]
    public void CreateDescribeRequest_WithTypeName_SetsParameter()
    {
        var request = TypeCommand.CreateDescribeRequest("UnityEngine.Rigidbody");

        Assert.NotNull(request.Parameters);
        Assert.Equal("UnityEngine.Rigidbody", request.Parameters!["typeName"]?.GetValue<string>());
    }

    [Fact]
    public void CreateDescribeRequest_WithFull_SetsFullParameter()
    {
        var request = TypeCommand.CreateDescribeRequest("UnityEngine.Rigidbody", full: true);

        Assert.NotNull(request.Parameters);
        Assert.True(request.Parameters!["full"]?.GetValue<bool>());
    }

    [Fact]
    public void CreateDescribeRequest_WithoutFull_OmitsFullParameter()
    {
        var request = TypeCommand.CreateDescribeRequest("UnityEngine.Rigidbody", full: false);

        Assert.NotNull(request.Parameters);
        Assert.Null(request.Parameters!["full"]);
    }

    [Fact]
    public void CreateDescribeRequest_WithMaxMembers_SetsParameter()
    {
        var request = TypeCommand.CreateDescribeRequest("UnityEngine.Rigidbody", maxMembers: 10);

        Assert.NotNull(request.Parameters);
        Assert.Equal(10, request.Parameters!["maxMembers"]?.GetValue<int>());
    }

    [Fact]
    public void CreateDescribeRequest_WithoutMaxMembers_OmitsParameter()
    {
        var request = TypeCommand.CreateDescribeRequest("UnityEngine.Rigidbody", maxMembers: null);

        Assert.NotNull(request.Parameters);
        Assert.Null(request.Parameters!["maxMembers"]);
    }

    [Fact]
    public void CreateDescribeRequest_WithSimpleTypeName_SetsParameter()
    {
        var request = TypeCommand.CreateDescribeRequest("Rigidbody");

        Assert.NotNull(request.Parameters);
        Assert.Equal("Rigidbody", request.Parameters!["typeName"]?.GetValue<string>());
    }

    [Fact]
    public void CreateDescribeRequest_WithEmptyTypeName_Throws()
    {
        Assert.Throws<ArgumentException>(() => TypeCommand.CreateDescribeRequest(""));
    }

    [Fact]
    public void CreateDescribeRequest_WithWhitespaceTypeName_Throws()
    {
        Assert.Throws<ArgumentException>(() => TypeCommand.CreateDescribeRequest("   "));
    }

    [Fact]
    public void CreateDescribeRequest_WithFullAndMaxMembers_SetsBothParameters()
    {
        var request = TypeCommand.CreateDescribeRequest("UnityEngine.Vector3", full: true, maxMembers: 25);

        Assert.NotNull(request.Parameters);
        Assert.True(request.Parameters!["full"]?.GetValue<bool>());
        Assert.Equal(25, request.Parameters["maxMembers"]?.GetValue<int>());
    }
}
