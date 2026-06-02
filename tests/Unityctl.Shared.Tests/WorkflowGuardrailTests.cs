using Xunit;

namespace Unityctl.Shared.Tests;

public class WorkflowGuardrailTests
{
    [Fact]
    public void DotnetCi_RunsAllPrTestSuites_AsHardGate()
    {
        var source = ReadRepoFile(".github/workflows/ci-dotnet.yml");

        Assert.Contains("pull_request:", source);
        Assert.Contains("branches: [main, master]", source);
        Assert.Contains("os: [ubuntu-latest, windows-latest, macos-latest]", source);
        Assert.Contains("dotnet test tests/Unityctl.Shared.Tests --no-build -c Release", source);
        Assert.Contains("dotnet test tests/Unityctl.Core.Tests --no-build -c Release", source);
        Assert.Contains("dotnet test tests/Unityctl.Cli.Tests --no-build -c Release", source);
        Assert.Contains("dotnet test tests/Unityctl.Mcp.Tests --no-build -c Release", source);
        Assert.Contains("fail-fast: false", source);
        Assert.DoesNotContain("continue-on-error", source);
        Assert.DoesNotContain("|| true", source);
        AssertCiTestStepPrecedesPackagingAndSmoke(source);
        AssertDotnetTestCommandsDoNotFilterSuites(source);
    }

    [Fact]
    public void DotnetCi_RunsForEveryPrWithoutPathOrEventNarrowing()
    {
        var source = ReadRepoFile(".github/workflows/ci-dotnet.yml");

        Assert.Contains("pull_request:", source);
        Assert.DoesNotContain("paths:", source);
        Assert.DoesNotContain("paths-ignore:", source);
        Assert.DoesNotContain("branches-ignore:", source);
        Assert.DoesNotContain("types:", source);
        Assert.DoesNotContain("pull_request_target:", source);
    }

    [Fact]
    public void ReleaseWorkflow_DoesNotPublishWhenTestsFail()
    {
        var source = ReadRepoFile(".github/workflows/release.yml");
        var testStepIndex = source.IndexOf("- name: Test (unit + MCP)", StringComparison.Ordinal);
        var publishStepIndex = source.IndexOf("- name: Publish", StringComparison.Ordinal);
        var nugetPushIndex = source.IndexOf("- name: Push to NuGet.org", StringComparison.Ordinal);
        var releaseIndex = source.IndexOf("- name: Create GitHub Release", StringComparison.Ordinal);

        Assert.True(testStepIndex >= 0, "Release workflow must run unit + MCP tests.");
        Assert.True(publishStepIndex > testStepIndex, "Release packaging must happen after tests.");
        Assert.True(nugetPushIndex > testStepIndex, "NuGet push must happen after tests.");
        Assert.True(releaseIndex > testStepIndex, "GitHub Release creation must happen after tests.");
        Assert.Contains("dotnet test tests/Unityctl.Shared.Tests -c Release", source);
        Assert.Contains("dotnet test tests/Unityctl.Core.Tests -c Release", source);
        Assert.Contains("dotnet test tests/Unityctl.Cli.Tests -c Release", source);
        Assert.Contains("dotnet test tests/Unityctl.Mcp.Tests -c Release", source);
        Assert.DoesNotContain("continue-on-error", source);
        AssertDotnetTestCommandsDoNotFilterSuites(source);
    }

    [Fact]
    public void PublishedCliSmoke_CoversReadmeEntryPoints()
    {
        var source = ReadRepoFile(".github/workflows/ci-dotnet.yml");

        Assert.Contains("schema --format json", source);
        Assert.Contains("tools --json", source);
        Assert.Contains("> publish/schema.json", source);
        Assert.Contains("> publish/tools.json", source);
        Assert.Contains("function Invoke-UnityctlSmoke", source);
        Assert.Contains("[System.Diagnostics.ProcessStartInfo]::new()", source);
        Assert.Contains("$psi.ArgumentList.Add($argument)", source);
        Assert.Contains("$AllowedExitCodes -notcontains $process.ExitCode", source);
        Assert.Contains("published CLI help smoke", source);
        Assert.Contains("installed tool help smoke", source);
        Assert.Contains("-OutputPath \"publish/schema.json\"", source);
        Assert.Contains("-OutputPath \"publish/tool-schema.json\"", source);
        Assert.Contains("json.load(f)", source);
        Assert.Contains("@(\"doctor\", \"--project\", \"publish/smoke-project\", \"--json\")", source);
        Assert.Contains("@(\"check\", \"--project\", \"publish/smoke-project\", \"--type\", \"compile\", \"--json\")", source);
        Assert.Contains("@(\"workflow\", \"verify\", \"--file\", \"publish/smoke-verify.json\", \"--project\", \"publish/smoke-project\"", source);
        Assert.Contains("dotnet tool install unityctl --tool-path", source);
        Assert.Contains("installed tool check smoke", source);
        Assert.Contains("installed tool workflow verify smoke", source);
        Assert.Contains("installed tool schema and tools command names drifted", source);
        Assert.Contains("installed tool schema is missing required command", source);
        Assert.Contains("installed tool doctor JSON is missing", source);
        Assert.Contains("installed tool check JSON is missing", source);
        Assert.Contains("installed tool workflow verify JSON is missing", source);
        Assert.Contains("check JSON is missing", source);
        Assert.Contains("workflow verify JSON is missing", source);
    }

    [Fact]
    public void UnityIntegrationSmoke_ProvesLiveReadWriteAndWorkflowArtifacts()
    {
        var source = ReadRepoFile(".github/workflows/ci-unity.yml");

        Assert.Contains("schedule:", source);
        Assert.Contains("fail-fast: false", source);
        Assert.Contains("Verify Unity license secret", source);
        Assert.Contains("UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}", source);
        Assert.Contains("UNITY_SERIAL: ${{ secrets.UNITY_SERIAL }}", source);
        Assert.Contains("unityctl-live-artifacts/license-preflight.txt", source);
        Assert.Contains("unityctl-live-artifacts/planned-smoke.txt", source);
        Assert.Contains("Planned Unity live validation", source);
        Assert.Contains("player-settings set/get write-readback smoke", source);
        Assert.Contains("workflow verify projectValidate artifact smoke", source);
        Assert.Contains("Unity Integration requires either the UNITY_LICENSE or UNITY_SERIAL GitHub secret", source);
        Assert.Contains("unityctl check", source);
        Assert.Contains("unityctl scene hierarchy", source);
        Assert.Contains("unityctl player-settings set", source);
        Assert.Contains("unityctl player-settings get", source);
        Assert.Contains("unityctl workflow verify", source);
        Assert.Contains("player-settings readback mismatch", source);
        Assert.Contains("workflow verify did not pass", source);
        Assert.Contains("actions/upload-artifact@v6", source);
    }

    [Fact]
    public void ReadmeBadges_LinkToExactWorkflowPages()
    {
        AssertReadmeBadges(ReadRepoFile("README.md"));
        AssertReadmeBadges(ReadRepoFile("README.ko.md"));
    }

    [Fact]
    public void Readmes_LinkContributorTrustGuide()
    {
        Assert.Contains("[CONTRIBUTING.md](CONTRIBUTING.md)", ReadRepoFile("README.md"));
        Assert.Contains("[CONTRIBUTING.md](CONTRIBUTING.md)", ReadRepoFile("README.ko.md"));
    }

    [Fact]
    public void PublicTrustDocs_AdvertiseCurrentPrTestInventory()
    {
        var publicDocs = new[]
        {
            ReadRepoFile("README.md"),
            ReadRepoFile("README.ko.md"),
            ReadRepoFile("docs/ref/architecture-mermaid.md"),
            ReadRepoFile("docs/ref/getting-started.md"),
            ReadRepoFile("docs/status/README-SYNC-REPORT.md"),
            ReadRepoFile("docs/status/PROJECT-STATUS.md"),
        };

        foreach (var source in publicDocs)
        {
            Assert.DoesNotContain("476", source);
        }

        Assert.Contains("855 PR .NET tests", publicDocs[0]);
        Assert.Contains("855 PR .NET 테스트", publicDocs[1]);
        Assert.Contains("855 PR .NET xUnit tests", publicDocs[2]);
        Assert.Contains("855 PR .NET xUnit tests", publicDocs[3]);
        Assert.Contains("**855**", publicDocs[4]);
        Assert.Contains("**855개**", publicDocs[5]);
        Assert.Contains("Unity live blocker tracking issue: #17", publicDocs[4]);
        Assert.Contains("Configure Unity Integration Actions secret", publicDocs[4]);
    }

    [Fact]
    public void ReadmeSyncReport_TracksUnityLiveBlockerIssue()
    {
        var source = ReadRepoFile("docs/status/README-SYNC-REPORT.md");

        Assert.Contains("Unity live blocker tracking issue: #17", source);
        Assert.Contains("Configure Unity Integration Actions secret", source);
        Assert.Contains("gh secret list --repo Jason-hub-star/unityctl", source);
        Assert.Contains("NUGET_API_KEY", source);
        Assert.Contains("UNITY_LICENSE", source);
        Assert.Contains("UNITY_SERIAL", source);
        Assert.Contains("ci-unity.yml", source);
    }

    [Fact]
    public void PrDotnetSuites_DoNotHideFailuresBehindUndocumentedSkips()
    {
        var allowedSkip = "Skip = \"CLI assembly blocked by AppLocker policy. Skipping on restricted environment.\";";
        var testRoots = new[]
        {
            "tests/Unityctl.Shared.Tests",
            "tests/Unityctl.Core.Tests",
            "tests/Unityctl.Cli.Tests",
            "tests/Unityctl.Mcp.Tests",
        };
        var currentFile = Path.Combine(
            GetRepoRoot(),
            "tests",
            "Unityctl.Shared.Tests",
            "WorkflowGuardrailTests.cs");

        var offendingFiles = testRoots
            .SelectMany(root => Directory.EnumerateFiles(
                Path.Combine(GetRepoRoot(), root.Replace('/', Path.DirectorySeparatorChar)),
                "*.cs",
                SearchOption.AllDirectories))
            .Where(path => !string.Equals(path, currentFile, StringComparison.Ordinal))
            .Where(path =>
            {
                var source = File.ReadAllText(path);
                return source.Contains("Skip =", StringComparison.Ordinal)
                    && !source.Contains(allowedSkip, StringComparison.Ordinal);
            })
            .Select(path => Path.GetRelativePath(GetRepoRoot(), path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(offendingFiles);
    }

    private static string ReadRepoFile(string relativePath)
    {
        var path = Path.Combine(GetRepoRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        return File.ReadAllText(path);
    }

    private static void AssertReadmeBadges(string source)
    {
        Assert.Contains(
            "[![CI](https://github.com/Jason-hub-star/unityctl/actions/workflows/ci-dotnet.yml/badge.svg)](https://github.com/Jason-hub-star/unityctl/actions/workflows/ci-dotnet.yml)",
            source);
        Assert.Contains(
            "[![Unity Integration](https://github.com/Jason-hub-star/unityctl/actions/workflows/ci-unity.yml/badge.svg)](https://github.com/Jason-hub-star/unityctl/actions/workflows/ci-unity.yml)",
            source);
    }

    private static void AssertDotnetTestCommandsDoNotFilterSuites(string source)
    {
        var filteredCommands = source
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.StartsWith("dotnet test ", StringComparison.Ordinal))
            .Where(line => line.Contains("--filter", StringComparison.Ordinal)
                || line.Contains("--list-tests", StringComparison.Ordinal))
            .ToArray();

        Assert.Empty(filteredCommands);
    }

    private static void AssertCiTestStepPrecedesPackagingAndSmoke(string source)
    {
        var testStepIndex = source.IndexOf("- name: Test (unit + MCP, excluding Integration)", StringComparison.Ordinal);
        var publishStepIndex = source.IndexOf("- name: Publish CLI", StringComparison.Ordinal);
        var packStepIndex = source.IndexOf("- name: Pack CLI tool", StringComparison.Ordinal);
        var publishedSmokeIndex = source.IndexOf("- name: Smoke published CLI", StringComparison.Ordinal);
        var installedToolSmokeIndex = source.IndexOf("- name: Smoke local dotnet tool install", StringComparison.Ordinal);

        Assert.True(testStepIndex >= 0, "CI workflow must run Shared/Core/Cli/Mcp tests.");
        Assert.True(publishStepIndex > testStepIndex, "CI must run PR .NET tests before publishing the CLI.");
        Assert.True(packStepIndex > testStepIndex, "CI must run PR .NET tests before packing the CLI tool.");
        Assert.True(publishedSmokeIndex > testStepIndex, "CI must run PR .NET tests before published CLI smoke.");
        Assert.True(installedToolSmokeIndex > testStepIndex, "CI must run PR .NET tests before installed tool smoke.");
    }

    private static string GetRepoRoot()
    {
        var baseDir = AppContext.BaseDirectory;
        return Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
    }
}
