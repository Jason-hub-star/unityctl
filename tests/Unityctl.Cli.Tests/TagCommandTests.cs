using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class TagCommandTests
{
    [CliTestFact]
    public void List_SetsCommandName()
    {
        var request = TagCommand.CreateListRequest();
        Assert.Equal(WellKnownCommands.TagList, request.Command);
    }

    [CliTestFact]
    public void List_HasRequestId()
    {
        var request = TagCommand.CreateListRequest();
        Assert.False(string.IsNullOrEmpty(request.RequestId));
    }

    [CliTestFact]
    public void Add_SetsCommandName()
    {
        var request = TagCommand.CreateAddRequest("Player");
        Assert.Equal(WellKnownCommands.TagAdd, request.Command);
    }

    [CliTestFact]
    public void Add_SetsNameParameter()
    {
        var request = TagCommand.CreateAddRequest("Enemy");
        Assert.Equal("Enemy", request.Parameters!["name"]?.GetValue<string>());
    }

    [CliTestFact]
    public void Add_EmptyName_Throws()
    {
        Assert.Throws<ArgumentException>(() => TagCommand.CreateAddRequest(""));
    }
}
