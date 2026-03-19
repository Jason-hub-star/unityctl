using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public sealed class BuildTargetCommandTests
{
    [CliTestFact]
    public void CreateSwitchRequest_HasCorrectCommandAndTarget()
    {
        var request = BuildTargetCommand.CreateSwitchRequest("Android");

        Assert.Equal(WellKnownCommands.BuildTargetSwitch, request.Command);
        Assert.Equal("Android", request.Parameters!["target"]?.GetValue<string>());
    }

    [CliTestFact]
    public void CreateSwitchRequest_EmptyTarget_Throws()
    {
        Assert.Throws<ArgumentException>(() => BuildTargetCommand.CreateSwitchRequest(string.Empty));
    }

    [CliTestFact]
    public async Task SwitchAsync_InvalidTimeout_Throws()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            BuildTargetCommand.SwitchAsync("C:/Project", "Android", 0, json: true));
    }
}
