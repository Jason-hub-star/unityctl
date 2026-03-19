using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class DefineSymbolsCommandTests
{
    [CliTestFact]
    public void Get_SetsCommandName()
    {
        var request = DefineSymbolsCommand.CreateGetRequest(null);
        Assert.Equal(WellKnownCommands.DefineSymbolsGet, request.Command);
    }

    [CliTestFact]
    public void Get_OmitsTargetWhenNull()
    {
        var request = DefineSymbolsCommand.CreateGetRequest(null);
        Assert.False(request.Parameters!.ContainsKey("target"));
    }

    [CliTestFact]
    public void Get_SetsTargetWhenProvided()
    {
        var request = DefineSymbolsCommand.CreateGetRequest("Standalone");
        Assert.Equal("Standalone", request.Parameters!["target"]?.GetValue<string>());
    }

    [CliTestFact]
    public void Set_SetsCommandName()
    {
        var request = DefineSymbolsCommand.CreateSetRequest("DEBUG;TEST", null);
        Assert.Equal(WellKnownCommands.DefineSymbolsSet, request.Command);
    }

    [CliTestFact]
    public void Set_SetsSymbolsParameter()
    {
        var request = DefineSymbolsCommand.CreateSetRequest("MY_SYMBOL", null);
        Assert.Equal("MY_SYMBOL", request.Parameters!["symbols"]?.GetValue<string>());
    }

    [CliTestFact]
    public void Set_NullSymbols_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => DefineSymbolsCommand.CreateSetRequest(null!, null));
    }

    [CliTestFact]
    public void Set_SetsTargetWhenProvided()
    {
        var request = DefineSymbolsCommand.CreateSetRequest("A;B", "Android");
        Assert.Equal("Android", request.Parameters!["target"]?.GetValue<string>());
    }

    [CliTestFact]
    public void AllRequests_HaveRequestId()
    {
        Assert.False(string.IsNullOrEmpty(DefineSymbolsCommand.CreateGetRequest(null).RequestId));
        Assert.False(string.IsNullOrEmpty(DefineSymbolsCommand.CreateSetRequest("X", null).RequestId));
    }
}
