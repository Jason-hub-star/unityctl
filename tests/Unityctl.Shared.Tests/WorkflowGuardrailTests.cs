using Xunit;

namespace Unityctl.Shared.Tests;

public class WorkflowGuardrailTests
{
    [Fact]
    public void DotnetCi_RunsAllPrTestSuites_AsHardGate()
    {
        var source = ReadRepoFile(".github/workflows/ci-dotnet.yml");

        Assert.Contains("dotnet test tests/Unityctl.Shared.Tests --no-build -c Release", source);
        Assert.Contains("dotnet test tests/Unityctl.Core.Tests --no-build -c Release", source);
        Assert.Contains("dotnet test tests/Unityctl.Cli.Tests --no-build -c Release", source);
        Assert.Contains("dotnet test tests/Unityctl.Mcp.Tests --no-build -c Release", source);
        Assert.Contains("fail-fast: false", source);
        Assert.DoesNotContain("continue-on-error", source);
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

    private static string GetRepoRoot()
    {
        var baseDir = AppContext.BaseDirectory;
        return Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
    }
}
