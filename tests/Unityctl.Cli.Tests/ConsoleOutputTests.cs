using Spectre.Console;
using Unityctl.Cli.Output;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public sealed class ConsoleOutputTests
{
    // Regression: status names like "Busy" landed inside Spectre markup brackets and crashed rendering (BUG-4).
    [Theory]
    [InlineData(StatusCode.Busy)]
    [InlineData(StatusCode.UnknownError)]
    public void PrintResponse_FailStatusName_IsPrintedLiterally(StatusCode code)
    {
        var output = CapturePrint(CommandResponse.Fail(code, "boom"));

        Assert.Contains($"FAIL [{code}]", output);
    }

    [Fact]
    public void PrintResponse_Accepted_PrintsLiteralStatusCode()
    {
        var response = new CommandResponse
        {
            StatusCode = StatusCode.Accepted,
            Success = true,
            Message = "queued"
        };

        var output = CapturePrint(response);

        Assert.Contains("ACCEPTED [104]", output);
    }

    private static string CapturePrint(CommandResponse response)
    {
        using var writer = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Out = new AnsiConsoleOutput(writer)
        });

        ConsoleOutput.PrintResponse(response, console);
        return writer.ToString();
    }
}
