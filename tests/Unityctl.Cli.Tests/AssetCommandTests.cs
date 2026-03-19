using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class AssetCommandTests
{
    [CliTestFact]
    public void Find_SetsCommandName()
    {
        var request = AssetCommand.CreateFindRequest("t:Scene", null, null);
        Assert.Equal(WellKnownCommands.AssetFind, request.Command);
    }

    [CliTestFact]
    public void Find_SetsParameters()
    {
        var request = AssetCommand.CreateFindRequest("t:Scene", "Assets/Scenes", 3);

        Assert.Equal("t:Scene", request.Parameters!["filter"]?.GetValue<string>());
        Assert.Equal("Assets/Scenes", request.Parameters["folder"]?.GetValue<string>());
        Assert.Equal(3, request.Parameters["limit"]?.GetValue<int>());
    }

    [CliTestFact]
    public void Find_EmptyFilter_Throws()
    {
        Assert.Throws<ArgumentException>(() => AssetCommand.CreateFindRequest("", null, null));
    }

    [CliTestFact]
    public void GetInfo_SetsCommandNameAndPath()
    {
        var request = AssetCommand.CreateGetInfoRequest("Assets/Scenes/Main.unity");
        Assert.Equal(WellKnownCommands.AssetGetInfo, request.Command);
        Assert.Equal("Assets/Scenes/Main.unity", request.Parameters!["path"]?.GetValue<string>());
    }

    [CliTestFact]
    public void GetInfo_EmptyPath_Throws()
    {
        Assert.Throws<ArgumentException>(() => AssetCommand.CreateGetInfoRequest(""));
    }

    [CliTestFact]
    public void GetDependencies_SetsCommandNameAndParameters()
    {
        var request = AssetCommand.CreateGetDependenciesRequest("Assets/Scenes/Main.unity", recursive: false);
        Assert.Equal(WellKnownCommands.AssetGetDependencies, request.Command);
        Assert.Equal("Assets/Scenes/Main.unity", request.Parameters!["path"]?.GetValue<string>());
        Assert.False(request.Parameters["recursive"]?.GetValue<bool>());
    }

    [CliTestFact]
    public void GetDependencies_EmptyPath_Throws()
    {
        Assert.Throws<ArgumentException>(() => AssetCommand.CreateGetDependenciesRequest("", true));
    }

    [CliTestFact]
    public void ReferenceGraph_SetsCommandNameAndPath()
    {
        var request = AssetCommand.CreateReferenceGraphRequest("Assets/Materials/My.mat");
        Assert.Equal(WellKnownCommands.AssetReferenceGraph, request.Command);
        Assert.Equal("Assets/Materials/My.mat", request.Parameters!["path"]?.GetValue<string>());
    }

    [CliTestFact]
    public void ReferenceGraph_EmptyPath_Throws()
    {
        Assert.Throws<ArgumentException>(() => AssetCommand.CreateReferenceGraphRequest(""));
    }

    [CliTestFact]
    public void ParseRecursive_ParsesTrueAndFalse()
    {
        Assert.True(AssetCommand.ParseRecursive("true"));
        Assert.False(AssetCommand.ParseRecursive("false"));
        Assert.True(AssetCommand.ParseRecursive("1"));
        Assert.False(AssetCommand.ParseRecursive("0"));
    }

    [CliTestFact]
    public void ParseRecursive_Invalid_Throws()
    {
        Assert.Throws<ArgumentException>(() => AssetCommand.ParseRecursive("maybe"));
    }

    [CliTestFact]
    public void GetLabels_SetsCommandNameAndPath()
    {
        var request = AssetCommand.CreateGetLabelsRequest("Assets/Textures/Road.png");
        Assert.Equal(WellKnownCommands.AssetGetLabels, request.Command);
        Assert.Equal("Assets/Textures/Road.png", request.Parameters!["path"]?.GetValue<string>());
    }

    [CliTestFact]
    public void GetLabels_EmptyPath_Throws()
    {
        Assert.Throws<ArgumentException>(() => AssetCommand.CreateGetLabelsRequest(""));
    }

    [CliTestFact]
    public void SetLabels_SetsCommandNameAndParameters()
    {
        var request = AssetCommand.CreateSetLabelsRequest("Assets/Textures/Road.png", "TestLabel,Important");
        Assert.Equal(WellKnownCommands.AssetSetLabels, request.Command);
        Assert.Equal("Assets/Textures/Road.png", request.Parameters!["path"]?.GetValue<string>());
        Assert.Equal("TestLabel,Important", request.Parameters["labels"]?.GetValue<string>());
    }

    [CliTestFact]
    public void SetLabels_EmptyPath_Throws()
    {
        Assert.Throws<ArgumentException>(() => AssetCommand.CreateSetLabelsRequest("", "label"));
    }

    [CliTestFact]
    public void SetLabels_NullLabels_Throws()
    {
        Assert.Throws<ArgumentException>(() => AssetCommand.CreateSetLabelsRequest("Assets/x.png", null!));
    }
}
