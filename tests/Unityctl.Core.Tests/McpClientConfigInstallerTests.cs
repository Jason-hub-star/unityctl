using System.Text.Json.Nodes;
using Unityctl.Core.Setup;
using Xunit;

namespace Unityctl.Core.Tests;

public sealed class McpClientConfigInstallerTests : IDisposable
{
    private readonly string _root;
    private readonly string _home;
    private readonly string _project;

    public McpClientConfigInstallerTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "unityctl-mcpinstall-" + Guid.NewGuid().ToString("N"));
        _home = Path.Combine(_root, "home");
        _project = Path.Combine(_root, "project");
        Directory.CreateDirectory(_home);
        Directory.CreateDirectory(_project);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private McpClientConfigInstaller Installer() => new(_home);

    [Fact]
    public void Install_ClaudeCodeWithProject_CreatesProjectScopedMcpJson()
    {
        var result = Installer().Install("claude-code", _project);

        Assert.True(result.Success);
        Assert.True(result.FileCreated);
        Assert.False(result.AlreadyPresent);
        Assert.Equal(Path.Combine(_project, ".mcp.json"), result.ConfigPath);

        var root = JsonNode.Parse(File.ReadAllText(result.ConfigPath))!.AsObject();
        Assert.Equal("unityctl-mcp", root["mcpServers"]!["unityctl"]!["command"]!.GetValue<string>());
    }

    [Fact]
    public void Install_ClaudeCodeWithoutProject_TargetsUserLevelConfig()
    {
        var result = Installer().Install("claude-code");

        Assert.True(result.Success);
        Assert.Equal(Path.Combine(_home, ".claude.json"), result.ConfigPath);
    }

    [Fact]
    public void Install_PreservesUnrelatedServersAndTopLevelKeys()
    {
        var configPath = Path.Combine(_project, ".mcp.json");
        File.WriteAllText(configPath, """
        {
          "someOtherSetting": { "keepMe": true },
          "mcpServers": {
            "playwright": { "command": "npx", "args": ["@playwright/mcp"] }
          }
        }
        """);

        var result = Installer().Install("claude-code", _project);

        Assert.True(result.Success);
        Assert.False(result.FileCreated);

        var root = JsonNode.Parse(File.ReadAllText(configPath))!.AsObject();
        Assert.True(root["someOtherSetting"]!["keepMe"]!.GetValue<bool>());
        Assert.Equal("npx", root["mcpServers"]!["playwright"]!["command"]!.GetValue<string>());
        Assert.Equal("unityctl-mcp", root["mcpServers"]!["unityctl"]!["command"]!.GetValue<string>());
    }

    [Fact]
    public void Install_ExistingUnityctlEntry_IsReplacedAndReported()
    {
        var configPath = Path.Combine(_project, ".mcp.json");
        File.WriteAllText(configPath, """
        {
          "mcpServers": {
            "unityctl": { "command": "/old/path/unityctl-mcp" },
            "other": { "command": "keep" }
          }
        }
        """);

        var result = Installer().Install("claude-code", _project);

        Assert.True(result.Success);
        Assert.True(result.AlreadyPresent);

        var root = JsonNode.Parse(File.ReadAllText(configPath))!.AsObject();
        Assert.Equal("unityctl-mcp", root["mcpServers"]!["unityctl"]!["command"]!.GetValue<string>());
        Assert.Equal("keep", root["mcpServers"]!["other"]!["command"]!.GetValue<string>());
    }

    [Fact]
    public void Install_VsCode_UsesServersKeyWithStdioType()
    {
        var result = Installer().Install("vscode", _project);

        Assert.True(result.Success);
        Assert.Equal(Path.Combine(_project, ".vscode", "mcp.json"), result.ConfigPath);

        var root = JsonNode.Parse(File.ReadAllText(result.ConfigPath))!.AsObject();
        Assert.Null(root["mcpServers"]);
        Assert.Equal("stdio", root["servers"]!["unityctl"]!["type"]!.GetValue<string>());
        Assert.Equal("unityctl-mcp", root["servers"]!["unityctl"]!["command"]!.GetValue<string>());
    }

    [Fact]
    public void Install_VsCodeWithoutProject_FailsWithExplanation()
    {
        var result = Installer().Install("vscode");

        Assert.False(result.Success);
        Assert.Contains("--project", result.Message);
    }

    [Fact]
    public void Install_Cursor_WritesCursorMcpJson()
    {
        var result = Installer().Install("cursor");

        Assert.True(result.Success);
        Assert.Equal(Path.Combine(_home, ".cursor", "mcp.json"), result.ConfigPath);
        Assert.True(File.Exists(result.ConfigPath));
    }

    [Fact]
    public void Install_Codex_AppendsTomlTableAndKeepsExistingContent()
    {
        var configPath = Path.Combine(_home, ".codex", "config.toml");
        Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
        File.WriteAllText(configPath, "model = \"gpt-5.4\"\n\n[mcp_servers.playwright]\ncommand = \"npx\"\n");

        var result = Installer().Install("codex");

        Assert.True(result.Success);
        Assert.False(result.AlreadyPresent);

        var written = File.ReadAllText(configPath);
        Assert.Contains("model = \"gpt-5.4\"", written);
        Assert.Contains("[mcp_servers.playwright]", written);
        Assert.Contains("[mcp_servers.unityctl]", written);
        Assert.Contains("command = \"unityctl-mcp\"", written);
    }

    [Fact]
    public void Install_CodexTwice_IsIdempotent()
    {
        var installer = Installer();
        installer.Install("codex");
        var second = installer.Install("codex");

        Assert.True(second.Success);
        Assert.True(second.AlreadyPresent);

        var written = File.ReadAllText(second.ConfigPath);
        var occurrences = written.Split("[mcp_servers.unityctl]").Length - 1;
        Assert.Equal(1, occurrences);
    }

    [Fact]
    public void Install_CodexReplacesOnlyItsOwnTable()
    {
        var configPath = Path.Combine(_home, ".codex", "config.toml");
        Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
        File.WriteAllText(configPath,
            "[mcp_servers.unityctl]\ncommand = \"/stale/unityctl-mcp\"\nextra = 1\n\n[mcp_servers.other]\ncommand = \"keep\"\n");

        var result = Installer().Install("codex");

        Assert.True(result.Success);
        var written = File.ReadAllText(configPath);
        Assert.DoesNotContain("/stale/unityctl-mcp", written);
        Assert.DoesNotContain("extra = 1", written);
        Assert.Contains("[mcp_servers.other]", written);
        Assert.Contains("command = \"keep\"", written);
    }

    [Fact]
    public void Install_UnresolvableHome_FailsInsteadOfWritingToCurrentDirectory()
    {
        // Regression: a stripped environment made GetFolderPath return "", so
        // Path.Combine("", ".cursor", "mcp.json") wrote into the working directory.
        var installer = new McpClientConfigInstaller(homeDirectory: "not-an-absolute-home");
        var cwd = Directory.GetCurrentDirectory();

        var result = installer.Install("cursor");

        Assert.False(result.Success);
        Assert.Contains("home directory", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(Path.Combine(cwd, "not-an-absolute-home", ".cursor", "mcp.json")));
        Assert.False(File.Exists(Path.Combine(cwd, ".cursor", "mcp.json")));
    }

    [Fact]
    public void Install_ReplacingAnEntry_ReportsWhatItReplaced()
    {
        var configPath = Path.Combine(_project, ".mcp.json");
        File.WriteAllText(configPath, """
        {
          "mcpServers": {
            "unityctl": { "command": "/hand/tuned/unityctl-mcp", "args": ["--verbose"] }
          }
        }
        """);

        var result = Installer().Install("claude-code", _project);

        Assert.True(result.AlreadyPresent);
        Assert.NotNull(result.PreviousEntry);
        Assert.Contains("/hand/tuned/unityctl-mcp", result.PreviousEntry);
        Assert.Contains("--verbose", result.PreviousEntry);
    }

    [Fact]
    public void Install_CodexReplacingATable_ReportsThePreviousTable()
    {
        var configPath = Path.Combine(_home, ".codex", "config.toml");
        Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
        File.WriteAllText(configPath, "[mcp_servers.unityctl]\ncommand = \"/hand/tuned/unityctl-mcp\"\n");

        var result = Installer().Install("codex");

        Assert.True(result.AlreadyPresent);
        Assert.Contains("/hand/tuned/unityctl-mcp", result.PreviousEntry);
    }

    [Fact]
    public void Install_FreshEntry_HasNoPreviousEntry()
    {
        var result = Installer().Install("claude-code", _project);

        Assert.True(result.Success);
        Assert.Null(result.PreviousEntry);
    }

    [Fact]
    public void Install_ResolvedConfigPath_IsAlwaysAbsolute()
    {
        foreach (var client in McpClientConfigInstaller.SupportedClients)
        {
            var result = Installer().Install(client, _project, dryRun: true);
            Assert.True(result.Success, client);
            Assert.True(Path.IsPathRooted(result.ConfigPath), $"{client}: {result.ConfigPath}");
        }
    }

    [Fact]
    public void Install_UnknownClient_FailsWithCandidateList()
    {
        var result = Installer().Install("windsurf");

        Assert.False(result.Success);
        Assert.NotNull(result.Candidates);
        Assert.Equal(McpClientConfigInstaller.SupportedClients, result.Candidates);
        Assert.Contains("claude-code", result.Message);
    }

    [Fact]
    public void Install_DryRun_DoesNotTouchDisk()
    {
        var result = Installer().Install("claude-code", _project, dryRun: true);

        Assert.True(result.Success);
        Assert.False(File.Exists(Path.Combine(_project, ".mcp.json")));
        Assert.Contains("unityctl-mcp", result.Content);
    }

    [Fact]
    public void Install_MalformedJson_RefusesToClobber()
    {
        var configPath = Path.Combine(_project, ".mcp.json");
        File.WriteAllText(configPath, "{ this is not json");

        var result = Installer().Install("claude-code", _project);

        Assert.False(result.Success);
        Assert.Contains("not valid JSON", result.Message);
        Assert.Equal("{ this is not json", File.ReadAllText(configPath));
    }

    [Fact]
    public void Install_Entry_ReportsOnlyOurServerNotTheWholeConfig()
    {
        var configPath = Path.Combine(_project, ".mcp.json");
        File.WriteAllText(configPath, """
        {
          "numStartups": 1930,
          "oauthAccount": { "emailAddress": "someone@example.com" },
          "mcpServers": { "playwright": { "command": "npx" } }
        }
        """);

        var result = Installer().Install("claude-code", _project);

        Assert.True(result.Success);
        // Content is what lands on disk and keeps everything.
        Assert.Contains("numStartups", result.Content);
        // Entry is what we report back — it must not carry the user's state.
        Assert.DoesNotContain("numStartups", result.Entry);
        Assert.DoesNotContain("someone@example.com", result.Entry);
        Assert.DoesNotContain("playwright", result.Entry);
        Assert.Contains("unityctl-mcp", result.Entry);
    }

    [Fact]
    public void Install_CustomCommand_IsRegistered()
    {
        var result = Installer().Install("claude-code", _project, command: "/opt/bin/unityctl-mcp");

        Assert.True(result.Success);
        var root = JsonNode.Parse(File.ReadAllText(result.ConfigPath))!.AsObject();
        Assert.Equal("/opt/bin/unityctl-mcp", root["mcpServers"]!["unityctl"]!["command"]!.GetValue<string>());
    }
}
