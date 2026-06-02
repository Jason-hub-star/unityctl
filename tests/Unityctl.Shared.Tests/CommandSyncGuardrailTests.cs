using System.Reflection;
using System.Text.RegularExpressions;
using Unityctl.Shared.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Shared.Tests;

public class CommandSyncGuardrailTests
{
    private static readonly Regex AppAddRegex = new(@"app\.Add\(""([^""]+)""", RegexOptions.Compiled);
    private static readonly Regex WellKnownRefRegex = new(@"WellKnownCommands\.(\w+)", RegexOptions.Compiled);
    private static readonly Regex PluginConstRegex = new(@"public const string (\w+) = ""([^""]+)"";", RegexOptions.Compiled);
    private static readonly Regex PluginHandlerRegex = new(@"CommandName\s*=>\s*WellKnownCommands\.(\w+)", RegexOptions.Compiled);
    private static readonly Regex SharedJsonPropertyRegex = new(@"\[JsonPropertyName\(""([^""]+)""\)\]\s*public\s+[^{};]+?\s+(\w+)\s*\{", RegexOptions.Compiled);
    private static readonly Regex PluginJsonPropertyRegex = new(@"\[JsonProperty\(""([^""]+)""\)\]\s*public\s+[^;]+?\s+(\w+)\s*(?:;|=)", RegexOptions.Compiled);
    private static readonly Regex EnumMemberRegex = new(@"^\s*(\w+)\s*=\s*(-?\d+)\s*,?", RegexOptions.Compiled | RegexOptions.Multiline);

    [Fact]
    public void PluginSharedWellKnownCommands_CopyMatchesSharedDefinition()
    {
        var expected = GetSharedWellKnownConstants()
            .Where(pair => pair.Key is not nameof(WellKnownCommands.Schema)
                and not nameof(WellKnownCommands.Workflow))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var pluginCopy = ParsePluginWellKnownConstants();

        Assert.Equal(expected, pluginCopy);
    }

    [Fact]
    public void PluginSharedWireDtoFields_MatchSharedJsonContracts()
    {
        Assert.Equal(
            ParseSharedJsonPropertyNames(@"src\Unityctl.Shared\Protocol\CommandRequest.cs"),
            ParsePluginJsonPropertyNames(@"src\Unityctl.Plugin\Editor\Shared\CommandRequest.cs"));
        Assert.Equal(
            ParseSharedJsonPropertyNames(@"src\Unityctl.Shared\Protocol\CommandResponse.cs"),
            ParsePluginJsonPropertyNames(@"src\Unityctl.Plugin\Editor\Shared\CommandResponse.cs"));
        Assert.Equal(
            ParseSharedJsonPropertyNames(@"src\Unityctl.Shared\Protocol\EventEnvelope.cs"),
            ParsePluginJsonPropertyNames(@"src\Unityctl.Plugin\Editor\Shared\EventEnvelope.cs"));
        Assert.Equal(
            ParseSharedJsonPropertyNames(@"src\Unityctl.Shared\Protocol\PreflightCheck.cs"),
            ParsePluginJsonPropertyNames(@"src\Unityctl.Plugin\Editor\Shared\PreflightCheck.cs"));
    }

    [Fact]
    public void PluginSharedStatusCode_CopyMatchesSharedDefinition()
    {
        Assert.Equal(
            ParseEnumMembers(@"src\Unityctl.Shared\Protocol\StatusCode.cs"),
            ParseEnumMembers(@"src\Unityctl.Plugin\Editor\Shared\StatusCode.cs"));
    }

    [Fact]
    public void PluginSharedExecExpressionParser_PreservesCoreGrammarSentinels()
    {
        var shared = ReadRepoFile(@"src\Unityctl.Shared\Exec\ExecExpressionParser.cs");
        var plugin = ReadRepoFile(@"src\Unityctl.Plugin\Editor\Shared\ExecExpressionParser.cs");

        foreach (var sentinel in new[]
        {
            "expression must not be empty.",
            "expected a member path before '='.",
            "expected a value after '='.",
            "expected 'TypeName.MemberName'.",
            "unterminated string or bracketed expression.",
            "unterminated string or bracketed argument.",
            "empty arguments are not allowed.",
            "FindTopLevelAssignment",
            "FindInvocationOpenParen",
            "SplitArguments",
            "LastTopLevelOpenParenIndex"
        })
        {
            Assert.Contains(sentinel, shared);
            Assert.Contains(sentinel, plugin);
        }
    }

    [Fact]
    public void PluginCommandHandlers_CoverAllTransportCommands()
    {
        var expectedFields = ParsePluginWellKnownConstants()
            .Keys
            .Where(field => field is not nameof(WellKnownCommands.Watch))
            .OrderBy(field => field, StringComparer.Ordinal)
            .ToArray();

        var actualFields = ParsePluginHandlerFieldNames()
            .OrderBy(field => field, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expectedFields, actualFields);
    }

    [Fact]
    public void WatchCommand_UsesDedicatedIpcPath_InPlugin()
    {
        var source = ReadRepoFile(@"src\Unityctl.Plugin\Editor\Ipc\IpcServer.cs");

        Assert.Contains("WellKnownCommands.Watch", source);
        Assert.Contains("watch session started", source);
    }

    [Fact]
    public void IpcServer_UsesFastQuitShutdownPath()
    {
        var source = ReadRepoFile(@"src\Unityctl.Plugin\Editor\Ipc\IpcServer.cs");

        Assert.Contains("private void StopForEditorQuit()", source);
        Assert.Contains("StopInternal(ShutdownMode.EditorQuit)", source);
        Assert.Contains("if (!fastExit)", source);
    }

    [Fact]
    public void ScriptCommands_AreRegisteredAcrossCliMcpAndPlugin()
    {
        var cliCommands = ParseCliCommands();
        Assert.Contains("script get-errors", cliCommands);
        Assert.Contains("script find-refs", cliCommands);
        Assert.Contains("script rename-symbol", cliCommands);

        var queryAllowlist = ParseWellKnownFieldReferences(@"src\Unityctl.Mcp\Tools\QueryTool.cs");
        Assert.Contains(nameof(WellKnownCommands.ScriptGetErrors), queryAllowlist);
        Assert.Contains(nameof(WellKnownCommands.ScriptFindRefs), queryAllowlist);
        Assert.DoesNotContain(nameof(WellKnownCommands.ScriptRenameSymbol), queryAllowlist);

        var runAllowlist = ParseWellKnownFieldReferences(@"src\Unityctl.Mcp\Tools\RunTool.cs");
        Assert.Contains(nameof(WellKnownCommands.ScriptRenameSymbol), runAllowlist);

        var pluginHandlers = ParsePluginHandlerFieldNames();
        Assert.Contains(nameof(WellKnownCommands.ScriptGetErrors), pluginHandlers);
        Assert.Contains(nameof(WellKnownCommands.ScriptFindRefs), pluginHandlers);
        Assert.Contains(nameof(WellKnownCommands.ScriptRenameSymbol), pluginHandlers);
    }

    [Fact]
    public void UiReadCommands_AreRegisteredAcrossCliMcpAndPlugin()
    {
        var cliCommands = ParseCliCommands();
        Assert.Contains("ui find", cliCommands);
        Assert.Contains("ui get", cliCommands);

        var queryAllowlist = ParseWellKnownFieldReferences(@"src\Unityctl.Mcp\Tools\QueryTool.cs");
        Assert.Contains(nameof(WellKnownCommands.UiFind), queryAllowlist);
        Assert.Contains(nameof(WellKnownCommands.UiGet), queryAllowlist);

        var pluginHandlers = ParsePluginHandlerFieldNames();
        Assert.Contains(nameof(WellKnownCommands.UiFind), pluginHandlers);
        Assert.Contains(nameof(WellKnownCommands.UiGet), pluginHandlers);
    }

    [Fact]
    public void UiInteractionCommands_AreRegisteredAcrossCliMcpAndPlugin()
    {
        var cliCommands = ParseCliCommands();
        Assert.Contains("ui click", cliCommands);
        Assert.Contains("ui toggle", cliCommands);
        Assert.Contains("ui input", cliCommands);

        var runAllowlist = ParseWellKnownFieldReferences(@"src\Unityctl.Mcp\Tools\RunTool.cs");
        Assert.Contains(nameof(WellKnownCommands.UiClick), runAllowlist);
        Assert.Contains(nameof(WellKnownCommands.UiToggle), runAllowlist);
        Assert.Contains(nameof(WellKnownCommands.UiInput), runAllowlist);

        var pluginHandlers = ParsePluginHandlerFieldNames();
        Assert.Contains(nameof(WellKnownCommands.UiClick), pluginHandlers);
        Assert.Contains(nameof(WellKnownCommands.UiToggle), pluginHandlers);
        Assert.Contains(nameof(WellKnownCommands.UiInput), pluginHandlers);
    }

    [Fact]
    public void UitkCommands_AreRegisteredAcrossCliMcpAndPlugin()
    {
        var cliCommands = ParseCliCommands();
        Assert.Contains("uitk find", cliCommands);
        Assert.Contains("uitk get", cliCommands);
        Assert.Contains("uitk set-value", cliCommands);
        Assert.Contains("uitk click", cliCommands);

        var queryAllowlist = ParseWellKnownFieldReferences(@"src\Unityctl.Mcp\Tools\QueryTool.cs");
        Assert.Contains(nameof(WellKnownCommands.UitkFind), queryAllowlist);
        Assert.Contains(nameof(WellKnownCommands.UitkGet), queryAllowlist);

        var runAllowlist = ParseWellKnownFieldReferences(@"src\Unityctl.Mcp\Tools\RunTool.cs");
        Assert.Contains(nameof(WellKnownCommands.UitkSetValue), runAllowlist);
        Assert.Contains(nameof(WellKnownCommands.UitkClick), runAllowlist);

        var pluginHandlers = ParsePluginHandlerFieldNames();
        Assert.Contains(nameof(WellKnownCommands.UitkFind), pluginHandlers);
        Assert.Contains(nameof(WellKnownCommands.UitkGet), pluginHandlers);
        Assert.Contains(nameof(WellKnownCommands.UitkSetValue), pluginHandlers);
        Assert.Contains(nameof(WellKnownCommands.UitkClick), pluginHandlers);
    }

    [Fact]
    public void ExecStructuredCommands_AreRegisteredAcrossCliMcpAndPlugin()
    {
        var cliCommands = ParseCliCommands();
        Assert.Contains("exec list-callables", cliCommands);
        Assert.Contains("exec invoke", cliCommands);

        var queryAllowlist = ParseWellKnownFieldReferences(@"src\Unityctl.Mcp\Tools\QueryTool.cs");
        Assert.Contains(nameof(WellKnownCommands.ExecListCallables), queryAllowlist);

        var runAllowlist = ParseWellKnownFieldReferences(@"src\Unityctl.Mcp\Tools\RunTool.cs");
        Assert.Contains(nameof(WellKnownCommands.ExecInvoke), runAllowlist);

        var pluginHandlers = ParsePluginHandlerFieldNames();
        Assert.Contains(nameof(WellKnownCommands.ExecListCallables), pluginHandlers);
        Assert.Contains(nameof(WellKnownCommands.ExecInvoke), pluginHandlers);
    }

    [Fact]
    public void MeshCreatePrimitive_IsRegisteredAcrossCliMcpAndPlugin()
    {
        var cliCommands = ParseCliCommands();
        Assert.Contains("mesh create-primitive", cliCommands);

        var runAllowlist = ParseWellKnownFieldReferences(@"src\Unityctl.Mcp\Tools\RunTool.cs");
        Assert.Contains(nameof(WellKnownCommands.MeshCreatePrimitive), runAllowlist);

        var pluginHandlers = ParsePluginHandlerFieldNames();
        Assert.Contains(nameof(WellKnownCommands.MeshCreatePrimitive), pluginHandlers);
    }

    [Fact]
    public void TestResult_IsRegisteredAcrossCliAndPlugin()
    {
        var cliCommands = ParseCliCommands();
        Assert.Contains("test-result", cliCommands);

        var pluginHandlers = ParsePluginHandlerFieldNames();
        Assert.Contains(nameof(WellKnownCommands.TestResult), pluginHandlers);
    }

    [Fact]
    public void McpAllowlists_ReferenceSchemaDiscoverableCatalogCommands()
    {
        var catalogNames = CommandCatalog.All
            .Select(command => command.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var wellKnownConstants = GetSharedWellKnownConstants();
        var allowlistFields = ParseWellKnownFieldReferences(@"src\Unityctl.Mcp\Tools\QueryTool.cs")
            .Concat(ParseWellKnownFieldReferences(@"src\Unityctl.Mcp\Tools\RunTool.cs"))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(field => field, StringComparer.Ordinal)
            .ToArray();

        foreach (var field in allowlistFields)
        {
            Assert.True(
                wellKnownConstants.TryGetValue(field, out var commandName),
                $"MCP allowlist references unknown WellKnownCommands.{field}");
            Assert.Contains(commandName!, catalogNames);
        }
    }

    [Fact]
    public void PluginTransportHandlers_AreSchemaDiscoverableCatalogCommands()
    {
        var catalogNames = CommandCatalog.All
            .Select(command => command.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var wellKnownConstants = GetSharedWellKnownConstants();
        var handlerFields = ParsePluginHandlerFieldNames()
            .Where(field => field is not nameof(WellKnownCommands.BuildProfileSetActiveResult)
                and not nameof(WellKnownCommands.BuildTargetSwitchResult)
                and not nameof(WellKnownCommands.AssetRefreshResult)
                and not nameof(WellKnownCommands.ScriptValidateResult)
                and not nameof(WellKnownCommands.LightingBakeResult))
            .OrderBy(field => field, StringComparer.Ordinal)
            .ToArray();

        foreach (var field in handlerFields)
        {
            Assert.True(
                wellKnownConstants.TryGetValue(field, out var commandName),
                $"Plugin handler references unknown WellKnownCommands.{field}");
            Assert.Contains(commandName!, catalogNames);
        }
    }

    [Fact]
    public void CatalogCliNames_AreRegisteredInProgram()
    {
        var cliCommands = ParseCliCommands();
        var missing = CommandCatalog.All
            .Select(command => command.CliName ?? command.Name)
            .Where(commandName => !commandName.Contains('<', StringComparison.Ordinal)
                && commandName is not "player-settings")
            .Where(commandName => !cliCommands.Contains(commandName))
            .OrderBy(commandName => commandName, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(missing);
    }

    [Fact]
    public void CliAndPluginRegistrations_DoNotContainDuplicateCommandNames()
    {
        AssertNoDuplicates(
            ParseCliCommandRegistrations(),
            "Duplicate CLI app.Add command registration");
        AssertNoDuplicates(
            ParsePluginHandlerFieldReferences(),
            "Duplicate Plugin handler CommandName registration");
    }

    [Fact]
    public void CodePatterns_DocumentsCommandSyncChecklistAndFlakyPolicy()
    {
        var source = ReadRepoFile(@"docs\ref\code-patterns.md");

        Assert.Contains("### Flaky 테스트 정책", source);
        Assert.Contains("flaky 0개", source);
        Assert.Contains("FlightLogRobustnessTests.Query_FilterByUntil_ExcludesNewerEntries", source);
        Assert.Contains("IPC timeout, AppLocker, batch fallback, dirty scene policy, parser edge case", source);
        Assert.Contains(".github/ISSUE_TEMPLATE/flaky-test.yml", source);
        Assert.Contains(".github/ISSUE_TEMPLATE/regression-bug.yml", source);
        Assert.Contains("regression issue를 링크", source);
        Assert.Contains(".github/PULL_REQUEST_TEMPLATE.md", source);
        Assert.Contains("CONTRIBUTING.md", source);
        Assert.Contains("Plugin shared copy drift", source);
        Assert.Contains("shadow", source);

        Assert.Contains("### 새 명령 추가 체크리스트", source);
        Assert.Contains("WellKnownCommands", source);
        Assert.Contains("CommandCatalog", source);
        Assert.Contains("src/Unityctl.Cli/Program.cs", source);
        Assert.Contains("QueryTool", source);
        Assert.Contains("RunTool", source);
        Assert.Contains("src/Unityctl.Plugin/Editor/Commands/*Handler.cs", source);
        Assert.Contains("CommandSyncGuardrailTests", source);
    }

    [Fact]
    public void IssueTemplates_CaptureFlakyAndRegressionEvidence()
    {
        var flaky = ReadRepoFile(@".github\ISSUE_TEMPLATE\flaky-test.yml");
        Assert.Contains("labels: [\"flaky-test\", \"test-trust\"]", flaky);
        Assert.Contains("Test name", flaky);
        Assert.Contains("CI evidence", flaky);
        Assert.Contains("Repeatability", flaky);
        Assert.Contains("Isolation or stabilization plan", flaky);
        Assert.Contains("Unityctl.Core.Tests.Namespace.ClassName.TestName", flaky);
        Assert.DoesNotContain("FlightLogRobustnessTests.Query_FilterByUntil_ExcludesNewerEntries", flaky);

        var regression = ReadRepoFile(@".github\ISSUE_TEMPLATE\regression-bug.yml");
        Assert.Contains("labels: [\"regression\", \"needs-repro-test\"]", regression);
        Assert.Contains("IPC timeout", regression);
        Assert.Contains("AppLocker", regression);
        Assert.Contains("batch fallback", regression);
        Assert.Contains("dirty scene policy", regression);
        Assert.Contains("parser edge case", regression);
        Assert.Contains("command/schema/plugin drift", regression);
        Assert.Contains("Required reproduction test", regression);
    }

    [Fact]
    public void PullRequestTemplate_CapturesTrustBaselineChecklist()
    {
        var source = ReadRepoFile(@".github\PULL_REQUEST_TEMPLATE.md");

        Assert.Contains("Test Trust Checklist", source);
        Assert.Contains("Shared/Core/Cli/Mcp on Linux, macOS, and Windows", source);
        Assert.Contains(".github/ISSUE_TEMPLATE/flaky-test.yml", source);
        Assert.Contains(".github/ISSUE_TEMPLATE/regression-bug.yml", source);
        Assert.Contains("link a `.github/ISSUE_TEMPLATE/regression-bug.yml` issue", source);

        Assert.Contains("Contract Safety Checklist", source);
        Assert.Contains("WellKnownCommands", source);
        Assert.Contains("CommandCatalog", source);
        Assert.Contains("src/Unityctl.Cli/Program.cs", source);
        Assert.Contains("QueryTool", source);
        Assert.Contains("RunTool", source);
        Assert.Contains("Plugin handler", source);
        Assert.Contains("duplicate or shadow", source);
        Assert.Contains("CommandSyncGuardrailTests", source);

        Assert.Contains("README User Path", source);
        Assert.Contains("dotnet tool install", source);
        Assert.Contains("unityctl tools --json", source);
        Assert.Contains("unityctl schema", source);
        Assert.Contains("doctor", source);
        Assert.Contains("check", source);
        Assert.Contains("workflow verify", source);

        Assert.Contains("Unity Reality Check", source);
        Assert.Contains("UNITY_LICENSE", source);
        Assert.Contains("UNITY_SERIAL", source);
        Assert.Contains("license-preflight.txt", source);
        Assert.Contains("planned-smoke.txt", source);
    }

    [Fact]
    public void ContributingGuide_CapturesPublicTestTrustPolicy()
    {
        var source = ReadRepoFile("CONTRIBUTING.md");

        Assert.Contains("dotnet test tests/Unityctl.Shared.Tests -c Release", source);
        Assert.Contains("dotnet test tests/Unityctl.Core.Tests -c Release", source);
        Assert.Contains("dotnet test tests/Unityctl.Cli.Tests -c Release", source);
        Assert.Contains("dotnet test tests/Unityctl.Mcp.Tests -c Release", source);
        Assert.Contains("pull_request", source);
        Assert.Contains("main`/`master", source);
        Assert.Contains("ubuntu-latest", source);
        Assert.Contains("windows-latest", source);
        Assert.Contains("macos-latest", source);
        Assert.Contains("fail-fast: false", source);
        Assert.Contains("continue-on-error", source);
        Assert.Contains(".github/ISSUE_TEMPLATE/flaky-test.yml", source);
        Assert.Contains(".github/ISSUE_TEMPLATE/regression-bug.yml", source);
        Assert.Contains("FlightLogRobustnessTests.Query_FilterByUntil_ExcludesNewerEntries", source);
        Assert.Contains("Resolved date/time boundary regressions", source);

        Assert.Contains("WellKnownCommands", source);
        Assert.Contains("CommandCatalog", source);
        Assert.Contains("src/Unityctl.Cli/Program.cs", source);
        Assert.Contains("QueryTool", source);
        Assert.Contains("RunTool", source);
        Assert.Contains("src/Unityctl.Plugin/Editor/Commands", source);
        Assert.Contains("CommandSyncGuardrailTests", source);
        Assert.Contains("Plugin shared copy drift", source);
        Assert.Contains("shadow a public command", source);

        Assert.Contains("dotnet tool install", source);
        Assert.Contains("unityctl tools --json", source);
        Assert.Contains("unityctl schema", source);
        Assert.Contains("workflow verify", source);
        Assert.Contains("UNITY_LICENSE", source);
        Assert.Contains("UNITY_SERIAL", source);
        Assert.Contains("license-preflight.txt", source);
        Assert.Contains("planned-smoke.txt", source);
        Assert.Contains("gh workflow run ci-unity.yml --ref <branch>", source);
        Assert.Contains("gh run watch <run-id> --exit-status", source);
        Assert.Contains("gh run download <run-id> --dir <artifact-dir>", source);
    }

    private static string ReadRepoFile(string relativePath)
    {
        var normalized = relativePath.Replace('\\', Path.DirectorySeparatorChar);
        var path = Path.Combine(GetRepoRoot(), normalized);
        return File.ReadAllText(path);
    }

    private static string GetRepoRoot()
    {
        var baseDir = AppContext.BaseDirectory;
        return Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
    }

    private static Dictionary<string, string> GetSharedWellKnownConstants()
    {
        return typeof(WellKnownCommands)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .ToDictionary(
                field => field.Name,
                field => (string)field.GetRawConstantValue()!,
                StringComparer.Ordinal);
    }

    private static Dictionary<string, string> ParsePluginWellKnownConstants()
    {
        var source = ReadRepoFile(@"src\Unityctl.Plugin\Editor\Shared\WellKnownCommands.cs");

        return PluginConstRegex
            .Matches(source)
            .Select(match => (Field: match.Groups[1].Value, Value: match.Groups[2].Value))
            .ToDictionary(item => item.Field, item => item.Value, StringComparer.Ordinal);
    }

    private static string[] ParseSharedJsonPropertyNames(string relativePath)
        => SharedJsonPropertyRegex
            .Matches(ReadRepoFile(relativePath))
            .Select(match => match.Groups[1].Value)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

    private static string[] ParsePluginJsonPropertyNames(string relativePath)
        => PluginJsonPropertyRegex
            .Matches(ReadRepoFile(relativePath))
            .Select(match => match.Groups[1].Value)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

    private static Dictionary<string, int> ParseEnumMembers(string relativePath)
        => EnumMemberRegex
            .Matches(ReadRepoFile(relativePath))
            .Select(match => (Name: match.Groups[1].Value, Value: int.Parse(match.Groups[2].Value)))
            .ToDictionary(item => item.Name, item => item.Value, StringComparer.Ordinal);

    private static HashSet<string> ParsePluginHandlerFieldNames()
        => ParsePluginHandlerFieldReferences()
            .ToHashSet(StringComparer.Ordinal);

    private static string[] ParsePluginHandlerFieldReferences()
    {
        var commandsDir = Path.Combine(GetRepoRoot(), "src", "Unityctl.Plugin", "Editor", "Commands");
        var files = Directory.GetFiles(commandsDir, "*Handler.cs", SearchOption.TopDirectoryOnly);

        return files
            .SelectMany(path => PluginHandlerRegex.Matches(File.ReadAllText(path)).Select(match => match.Groups[1].Value))
            .ToArray();
    }

    private static HashSet<string> ParseWellKnownFieldReferences(string relativePath)
    {
        var source = ReadRepoFile(relativePath);
        return WellKnownRefRegex
            .Matches(source)
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static HashSet<string> ParseCliCommands()
        => ParseCliCommandRegistrations()
            .ToHashSet(StringComparer.Ordinal);

    private static string[] ParseCliCommandRegistrations()
    {
        var source = ReadRepoFile(@"src\Unityctl.Cli\Program.cs");
        return AppAddRegex
            .Matches(source)
            .Select(match => match.Groups[1].Value)
            .ToArray();
    }

    private static void AssertNoDuplicates(string[] values, string message)
    {
        var duplicates = values
            .GroupBy(value => value, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.True(duplicates.Length == 0, $"{message}: {string.Join(", ", duplicates)}");
    }
}
