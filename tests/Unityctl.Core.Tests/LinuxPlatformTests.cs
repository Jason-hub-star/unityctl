using Unityctl.Core.Platform;
using Xunit;

namespace Unityctl.Core.Tests;

public class LinuxPlatformTests
{
    [Fact]
    public void IsProjectLocked_ReturnsFalse_WhenLockfileIsMissing()
    {
        using var tempDirectory = new TemporaryDirectory();
        var platform = new LinuxPlatform();

        var isLocked = platform.IsProjectLocked(tempDirectory.Path);

        Assert.False(isLocked);
    }

    [Fact]
    public void IsProjectLocked_ReturnsFalse_ForStaleLockfile()
    {
        using var tempDirectory = new TemporaryDirectory();
        var lockFile = CreateLockFile(tempDirectory.Path);
        var platform = new LinuxPlatform();

        var isLocked = platform.IsProjectLocked(tempDirectory.Path);

        Assert.False(isLocked);
    }

    [Fact]
    public void IsProjectLocked_ReturnsTrue_WhenLockfileIsHeldOpen()
    {
        using var tempDirectory = new TemporaryDirectory();
        var lockFile = CreateLockFile(tempDirectory.Path);
        using var heldHandle = File.Open(lockFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        var platform = new LinuxPlatform();

        var isLocked = platform.IsProjectLocked(tempDirectory.Path);

        Assert.True(isLocked);
    }

    private static string CreateLockFile(string projectPath)
    {
        var tempPath = Path.Combine(projectPath, "Temp");
        Directory.CreateDirectory(tempPath);

        var lockFile = Path.Combine(tempPath, "UnityLockfile");
        File.WriteAllText(lockFile, string.Empty);
        return lockFile;
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"unityctl-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
