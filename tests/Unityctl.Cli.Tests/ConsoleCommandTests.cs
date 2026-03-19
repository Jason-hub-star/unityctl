using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class ConsoleCommandTests
{
    [CliTestFact]
    public void Clear_SetsCommandName()
    {
        var request = ConsoleCommand.CreateClearRequest();
        Assert.Equal(WellKnownCommands.ConsoleClear, request.Command);
    }

    [CliTestFact]
    public void GetCount_SetsCommandName()
    {
        var request = ConsoleCommand.CreateGetCountRequest();
        Assert.Equal(WellKnownCommands.ConsoleGetCount, request.Command);
    }

    [CliTestFact]
    public void AllRequests_HaveRequestId()
    {
        Assert.False(string.IsNullOrEmpty(ConsoleCommand.CreateClearRequest().RequestId));
        Assert.False(string.IsNullOrEmpty(ConsoleCommand.CreateGetCountRequest().RequestId));
    }
}
