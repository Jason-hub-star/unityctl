using Unityctl.Core.Transport;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Core.Tests.Transport;

public sealed class CommandExecutorReadinessTests
{
    [Fact]
    public void BuildInteractiveBusyResponse_ForScriptGetErrors_AddsScriptSpecificGuidance()
    {
        var response = CommandExecutor.BuildInteractiveBusyResponse(
            @"C:\Users\gmdqn\robotapp",
            WellKnownCommands.ScriptGetErrors);

        Assert.Equal(StatusCode.Busy, response.StatusCode);
        Assert.Contains("script get-errors", response.Message);
        Assert.True(response.Data!["requiresIpcReady"]!.GetValue<bool>());
        Assert.Contains("script validate", response.Data["followUpAction"]!.GetValue<string>());
    }

    [Fact]
    public void BuildInteractiveBusyResponse_ForNonScriptCommand_KeepsGenericMessage()
    {
        var response = CommandExecutor.BuildInteractiveBusyResponse(
            @"C:\Users\gmdqn\robotapp",
            WellKnownCommands.Status);

        Assert.Equal(StatusCode.Busy, response.StatusCode);
        Assert.Contains("IPC is not ready", response.Message);
        Assert.Null(response.Data);
    }

    [Fact]
    public void BuildInteractiveBusyResponse_ForUiInput_AddsUiSpecificGuidance()
    {
        var response = CommandExecutor.BuildInteractiveBusyResponse(
            @"C:\Users\gmdqn\robotapp",
            WellKnownCommands.UiInput);

        Assert.Equal(StatusCode.Busy, response.StatusCode);
        Assert.Contains("ui input", response.Message);
        Assert.True(response.Data!["requiresIpcReady"]!.GetValue<bool>());
        Assert.Contains("deterministically", response.Data["followUpAction"]!.GetValue<string>());
    }
}
