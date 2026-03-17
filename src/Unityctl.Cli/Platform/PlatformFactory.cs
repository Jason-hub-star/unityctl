using System.Runtime.InteropServices;

namespace Unityctl.Cli.Platform;

public static class PlatformFactory
{
    public static IPlatformServices Create()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new WindowsPlatform();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new MacOsPlatform();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return new LinuxPlatform();

        throw new PlatformNotSupportedException(
            $"Unsupported OS: {RuntimeInformation.OSDescription}");
    }
}
