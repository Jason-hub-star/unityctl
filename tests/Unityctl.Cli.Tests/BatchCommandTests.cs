using System.Text.Json.Nodes;
using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public sealed class BatchCommandTests
{
    [CliTestFact]
    public void CreateExecuteRequest_UsesBatchExecuteCommand()
    {
        var request = BatchCommand.CreateExecuteRequest(
            "[{\"command\":\"gameobject-create\",\"parameters\":{\"name\":\"Cube\"}}]",
            file: null,
            rollbackOnFailure: true);

        Assert.Equal(WellKnownCommands.BatchExecute, request.Command);
    }

    [CliTestFact]
    public void CreateExecuteRequest_SetsRollbackAndCommands()
    {
        var request = BatchCommand.CreateExecuteRequest(
            "[{\"command\":\"gameobject-create\",\"parameters\":{\"name\":\"Cube\"}}]",
            file: null,
            rollbackOnFailure: false);

        Assert.False(request.Parameters!["rollbackOnFailure"]?.GetValue<bool>());

        var commands = request.Parameters["commands"]?.AsArray();
        Assert.NotNull(commands);
        Assert.Single(commands!);
        Assert.Equal("gameobject-create", commands[0]!["command"]?.GetValue<string>());
        Assert.Equal("Cube", commands[0]!["parameters"]!["name"]?.GetValue<string>());
    }

    [CliTestFact]
    public void CreateExecuteRequest_FromFile_LoadsCommands()
    {
        var file = Path.GetTempFileName();
        try
        {
            File.WriteAllText(file, "[{\"command\":\"gameobject-create\",\"parameters\":{\"name\":\"FromFile\"}}]");

            var request = BatchCommand.CreateExecuteRequest(
                commandsJson: null,
                file: file,
                rollbackOnFailure: true);

            var commands = request.Parameters!["commands"]?.AsArray();
            Assert.NotNull(commands);
            Assert.Equal("FromFile", commands![0]!["parameters"]!["name"]?.GetValue<string>());
        }
        finally
        {
            File.Delete(file);
        }
    }

    [CliTestFact]
    public void CreateExecuteRequest_RejectsMissingCommandsSource()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            BatchCommand.CreateExecuteRequest(commandsJson: null, file: null, rollbackOnFailure: true));

        Assert.Contains("Specify exactly one", ex.Message);
    }

    [CliTestFact]
    public void CreateExecuteRequest_RejectsBothCommandsAndFile()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            BatchCommand.CreateExecuteRequest("[]", "batch.json", rollbackOnFailure: true));

        Assert.Contains("Specify exactly one", ex.Message);
    }

    [CliTestFact]
    public void ParseCommands_RejectsNonArrayJson()
    {
        var ex = Assert.Throws<ArgumentException>(() => BatchCommand.ParseCommands("{\"command\":\"gameobject-create\"}"));
        Assert.Contains("JSON array", ex.Message);
    }

    [CliTestFact]
    public void AllRequests_HaveRequestId()
    {
        var request = BatchCommand.CreateExecuteRequest(
            "[{\"command\":\"gameobject-create\",\"parameters\":{\"name\":\"Cube\"}}]",
            file: null,
            rollbackOnFailure: true);

        Assert.False(string.IsNullOrEmpty(request.RequestId));
    }
}
