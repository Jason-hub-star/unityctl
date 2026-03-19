using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class PhysicsCommandTests
{
    [Fact]
    public void CreateGetSettingsRequest_HasCorrectCommand()
    {
        var request = PhysicsCommand.CreateGetSettingsRequest();
        Assert.Equal(WellKnownCommands.PhysicsGetSettings, request.Command);
        Assert.NotNull(request.RequestId);
    }

    [Fact]
    public void CreateSetSettingsRequest_HasCorrectCommand()
    {
        var request = PhysicsCommand.CreateSetSettingsRequest("m_Gravity", "[0,-20,0]");
        Assert.Equal(WellKnownCommands.PhysicsSetSettings, request.Command);
        Assert.NotNull(request.RequestId);
    }

    [Fact]
    public void CreateSetSettingsRequest_EmptyProperty_Throws()
    {
        Assert.Throws<ArgumentException>(() => PhysicsCommand.CreateSetSettingsRequest("", "[0,-9.81,0]"));
    }

    [Fact]
    public void CreateGetCollisionMatrixRequest_HasCorrectCommand()
    {
        var request = PhysicsCommand.CreateGetCollisionMatrixRequest();
        Assert.Equal(WellKnownCommands.PhysicsGetCollisionMatrix, request.Command);
        Assert.NotNull(request.RequestId);
    }

    [Fact]
    public void CreateSetCollisionMatrixRequest_HasCorrectCommand()
    {
        var request = PhysicsCommand.CreateSetCollisionMatrixRequest("8", "9", "true");
        Assert.Equal(WellKnownCommands.PhysicsSetCollisionMatrix, request.Command);
        Assert.NotNull(request.RequestId);
    }

    [Fact]
    public void CreateSetCollisionMatrixRequest_EmptyLayer1_Throws()
    {
        Assert.Throws<ArgumentException>(() => PhysicsCommand.CreateSetCollisionMatrixRequest("", "9", "true"));
    }

    [Fact]
    public void CreateSetCollisionMatrixRequest_EmptyLayer2_Throws()
    {
        Assert.Throws<ArgumentException>(() => PhysicsCommand.CreateSetCollisionMatrixRequest("8", "", "true"));
    }

    [Fact]
    public void CreateSetSettingsRequest_ParametersContainPropertyAndValue()
    {
        var request = PhysicsCommand.CreateSetSettingsRequest("m_DefaultSolverIterations", "12");
        Assert.Equal("m_DefaultSolverIterations", request.Parameters!["property"]!.ToString());
        Assert.Equal("12", request.Parameters!["value"]!.ToString());
    }

    [Fact]
    public void CreateSetCollisionMatrixRequest_ParametersContainAllFields()
    {
        var request = PhysicsCommand.CreateSetCollisionMatrixRequest("Water", "10", "false");
        Assert.Equal("Water", request.Parameters!["layer1"]!.ToString());
        Assert.Equal("10", request.Parameters!["layer2"]!.ToString());
        Assert.Equal("false", request.Parameters!["ignore"]!.ToString());
    }
}
