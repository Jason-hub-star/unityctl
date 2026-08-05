using Unityctl.Cli.Commands;
using Unityctl.Core.Setup;
using Xunit;

namespace Unityctl.Cli.Tests;

public sealed class McpCommandTests
{
    [Fact]
    public void BuildPayload_Success_ExposesConfigPathAndMergedPreview()
    {
        var result = new McpInstallResult(
            Success: true,
            Client: "claude-code",
            ConfigPath: "/tmp/project/.mcp.json",
            Format: McpConfigFormat.JsonMcpServers,
            FileCreated: true,
            AlreadyPresent: false,
            Entry: "{\"mcpServers\":{\"unityctl\":{\"command\":\"unityctl-mcp\"}}}",
            Content: "{\"unrelatedUserState\":\"…900KB…\",\"mcpServers\":{\"unityctl\":{\"command\":\"unityctl-mcp\"}}}",
            Message: "Would write unityctl MCP server entry in /tmp/project/.mcp.json");

        var payload = McpCommand.BuildPayload(result, dryRun: true);

        Assert.True(payload["success"]!.GetValue<bool>());
        var data = payload["data"]!.AsObject();
        Assert.Equal("claude-code", data["client"]!.GetValue<string>());
        Assert.Equal("/tmp/project/.mcp.json", data["configPath"]!.GetValue<string>());
        Assert.Equal("JsonMcpServers", data["format"]!.GetValue<string>());
        Assert.True(data["dryRun"]!.GetValue<bool>());
        Assert.Contains("unityctl-mcp", data["entry"]!.GetValue<string>());
        Assert.Equal(result.Content.Length, data["configBytes"]!.GetValue<int>());
        Assert.Null(data["candidates"]);

        // The payload must never echo the user's whole config file back to the agent.
        Assert.DoesNotContain("unrelatedUserState", payload.ToJsonString());
    }

    [Fact]
    public void BuildPayload_UnknownClient_ExposesCandidateList()
    {
        var result = new McpInstallResult(
            Success: false,
            Client: "windsurf",
            ConfigPath: string.Empty,
            Format: McpConfigFormat.JsonMcpServers,
            FileCreated: false,
            AlreadyPresent: false,
            Entry: string.Empty,
            Content: string.Empty,
            Message: "Unknown client 'windsurf'.",
            Candidates: McpClientConfigInstaller.SupportedClients);

        var payload = McpCommand.BuildPayload(result, dryRun: false);

        Assert.False(payload["success"]!.GetValue<bool>());
        var candidates = payload["data"]!["candidates"]!.AsArray();
        Assert.Equal(McpClientConfigInstaller.SupportedClients.Count, candidates.Count);
        Assert.Contains(candidates, node => node!.GetValue<string>() == "vscode");
    }
}
