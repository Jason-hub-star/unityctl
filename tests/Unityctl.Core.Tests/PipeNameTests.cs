using Unityctl.Shared;
using Xunit;
using System.Runtime.InteropServices;

namespace Unityctl.Core.Tests;

public class PipeNameTests
{
    [Fact]
    public void GetPipeName_StartsWithPrefix()
    {
        var name = Constants.GetPipeName("/some/project");
        Assert.StartsWith("unityctl_", name);
    }

    [Fact]
    public void GetPipeName_DeterministicForSamePath()
    {
        var name1 = Constants.GetPipeName("/some/project");
        var name2 = Constants.GetPipeName("/some/project");
        Assert.Equal(name1, name2);
    }

    [Fact]
    public void GetPipeName_DifferentForDifferentPaths()
    {
        var name1 = Constants.GetPipeName("/project/a");
        var name2 = Constants.GetPipeName("/project/b");
        Assert.NotEqual(name1, name2);
    }

    [Fact]
    public void NormalizeProjectPath_TrimsTrailingSlashes()
    {
        var withSlash = Constants.NormalizeProjectPath("/some/project/");
        var withMultiple = Constants.NormalizeProjectPath("/some/project///");
        Assert.False(withSlash.EndsWith("/"));
        Assert.False(withMultiple.EndsWith("/"));
        Assert.Equal(withSlash, withMultiple);
    }

    [Fact]
    public void NormalizeProjectPath_UnifiesSlashes()
    {
        var normalized = Constants.NormalizeProjectPath("/some/project");
        Assert.DoesNotContain("\\", normalized);
    }

    [Fact]
    public void NormalizeProjectPath_ConvertsBackslashesToForwardSlashes()
    {
        var path = Path.Combine(Path.GetTempPath(), "unityctl-path-test");
        var withBackslashes = path.Replace(Path.DirectorySeparatorChar, '\\');

        var normalized = Constants.NormalizeProjectPath(withBackslashes);

        Assert.DoesNotContain("\\", normalized);
    }

    [Fact]
    public void NormalizeProjectPath_IgnoresMixedSlashAndTrailingSeparatorDifferences()
    {
        var forwardSlashPath = "C:/Users/jason/My project";
        var mixedSlashPath = @"C:\Users/jason\My project\\";

        Assert.Equal(
            Constants.NormalizeProjectPath(forwardSlashPath),
            Constants.NormalizeProjectPath(mixedSlashPath));
    }

    [Fact]
    public void GetPipeName_IgnoresTrailingSlashDifferences()
    {
        var path = Path.Combine(Path.GetTempPath(), "unityctl-pipe-test");
        var withSlash = path + Path.DirectorySeparatorChar;
        var withMultipleSlashes = path + new string(Path.DirectorySeparatorChar, 3);

        Assert.Equal(Constants.GetPipeName(path), Constants.GetPipeName(withSlash));
        Assert.Equal(Constants.GetPipeName(path), Constants.GetPipeName(withMultipleSlashes));
    }

    [Fact]
    public void GetPipeName_IgnoresSlashDirectionDifferences()
    {
        var forwardSlashPath = "C:/Users/jason/My project";
        var backslashPath = @"C:\Users\jason\My project";

        Assert.Equal(Constants.GetPipeName(forwardSlashPath), Constants.GetPipeName(backslashPath));
    }

    [Fact]
    public void NormalizeProjectPath_CaseBehavior_IsPlatformExplicit()
    {
        const string projectPath = "UnityctlCaseProbe";
        var normalized = Constants.NormalizeProjectPath(projectPath);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Assert.Equal(normalized.ToLowerInvariant(), normalized);
        else
            Assert.Contains(projectPath, normalized, StringComparison.Ordinal);
    }

    [Fact]
    public void GetPipeName_CaseOnlyPathDifferences_FollowPlatformPolicy()
    {
        var lowerPath = Path.Combine(Path.GetTempPath(), "unityctl-case-probe");
        var upperPath = Path.Combine(Path.GetTempPath(), "UNITYCTL-CASE-PROBE");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Assert.Equal(Constants.GetPipeName(lowerPath), Constants.GetPipeName(upperPath));
        else
            Assert.NotEqual(Constants.GetPipeName(lowerPath), Constants.GetPipeName(upperPath));
    }

    [Fact]
    public void GetPipeName_HasCorrectLength()
    {
        var name = Constants.GetPipeName("/some/project");
        // "unityctl_" (9 chars) + 16 hex chars = 25
        Assert.Equal(25, name.Length);
    }
}
