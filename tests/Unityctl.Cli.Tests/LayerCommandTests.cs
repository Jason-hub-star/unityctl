using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class LayerCommandTests
{
    [CliTestFact]
    public void List_SetsCommandName()
    {
        var request = LayerCommand.CreateListRequest();
        Assert.Equal(WellKnownCommands.LayerList, request.Command);
    }

    [CliTestFact]
    public void List_HasRequestId()
    {
        var request = LayerCommand.CreateListRequest();
        Assert.False(string.IsNullOrEmpty(request.RequestId));
    }

    [CliTestFact]
    public void Set_SetsCommandName()
    {
        var request = LayerCommand.CreateSetRequest(8, "MyLayer");
        Assert.Equal(WellKnownCommands.LayerSet, request.Command);
    }

    [CliTestFact]
    public void Set_SetsIndexAndName()
    {
        var request = LayerCommand.CreateSetRequest(10, "TestLayer");
        Assert.Equal(10, request.Parameters!["index"]?.GetValue<int>());
        Assert.Equal("TestLayer", request.Parameters!["name"]?.GetValue<string>());
    }

    [CliTestFact]
    public void Set_IndexBelow8_Throws()
    {
        Assert.Throws<ArgumentException>(() => LayerCommand.CreateSetRequest(7, "Nope"));
    }

    [CliTestFact]
    public void Set_IndexAbove31_Throws()
    {
        Assert.Throws<ArgumentException>(() => LayerCommand.CreateSetRequest(32, "Nope"));
    }

    [CliTestFact]
    public void Set_EmptyName_Throws()
    {
        Assert.Throws<ArgumentException>(() => LayerCommand.CreateSetRequest(8, ""));
    }
}
