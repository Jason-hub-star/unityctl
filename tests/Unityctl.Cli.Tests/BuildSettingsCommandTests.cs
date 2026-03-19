using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public sealed class BuildSettingsCommandTests
{
    [CliTestFact]
    public void CreateGetScenesRequest_HasCorrectCommand()
    {
        var request = BuildSettingsCommand.CreateGetScenesRequest();

        Assert.Equal(WellKnownCommands.BuildSettingsGetScenes, request.Command);
    }

    [CliTestFact]
    public void CreateGetScenesRequest_HasRequestId()
    {
        var request = BuildSettingsCommand.CreateGetScenesRequest();

        Assert.False(string.IsNullOrEmpty(request.RequestId));
    }

    [CliTestFact]
    public void CreateGetScenesRequest_HasEmptyParametersObject()
    {
        var request = BuildSettingsCommand.CreateGetScenesRequest();

        Assert.NotNull(request.Parameters);
        Assert.Empty(request.Parameters!);
    }

    [CliTestFact]
    public void SetScenes_SetsCommandNameAndScenes()
    {
        var request = BuildSettingsCommand.CreateSetScenesRequest("Assets/Scenes/Main.unity,Assets/Scenes/Menu.unity");

        Assert.Equal(WellKnownCommands.BuildSettingsSetScenes, request.Command);
        Assert.Equal("Assets/Scenes/Main.unity,Assets/Scenes/Menu.unity", request.Parameters!["scenes"]?.GetValue<string>());
    }

    [CliTestFact]
    public void SetScenes_EmptyScenes_Throws()
    {
        Assert.Throws<ArgumentException>(() => BuildSettingsCommand.CreateSetScenesRequest(""));
    }
}
