using System.Runtime.InteropServices;
using Unityctl.Cli.Platform;
using Xunit;

namespace Unityctl.Cli.Tests;

public class PlatformFactoryTests
{
    [Fact]
    public void Create_ReturnsCorrectPlatform()
    {
        var platform = PlatformFactory.Create();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Assert.IsType<WindowsPlatform>(platform);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            Assert.IsType<MacOsPlatform>(platform);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            Assert.IsType<LinuxPlatform>(platform);
    }

    [Fact]
    public void TempResponseFilePath_IsUnique()
    {
        var platform = PlatformFactory.Create();
        var path1 = platform.GetTempResponseFilePath();
        var path2 = platform.GetTempResponseFilePath();

        Assert.NotEqual(path1, path2);
        Assert.EndsWith(".json", path1);
    }
}
