using System.Diagnostics;
using System.Text;
using Unityctl.Core.Platform;
using Xunit;

namespace Unityctl.Core.Tests;

public sealed class LinuxPlatformTests
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
        _ = CreateLockFile(tempDirectory.Path);
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

    [Fact]
    public void TryParseProcess_ParsesInteractiveEditorAndSpacedProjectPath()
    {
        var commandLine = Encoding.UTF8.GetBytes(
            "/home/jason/Unity/Hub/Editor/6000.3.16f1/Editor/Unity\0"
            + "-projectPath\0/home/jason/My Unity Project\0-logFile\0/tmp/editor.log\0");

        var process = LinuxPlatform.TryParseProcess(
            4242,
            "/home/jason/Unity/Hub/Editor/6000.3.16f1/Editor/Unity",
            commandLine);

        Assert.NotNull(process);
        Assert.Equal(4242, process.ProcessId);
        Assert.Equal("/home/jason/My Unity Project", process.ProjectPath);
        Assert.Equal("6000.3.16f1", process.Version);
        Assert.False(process.IsBatchMode);
        Assert.True(process.HasMainWindow);
        Assert.True(process.IsInteractiveEditor);
    }

    [Theory]
    [InlineData("-batchmode")]
    [InlineData("-nographics")]
    [InlineData("-adb2")]
    public void TryParseProcess_ClassifiesHeadlessUnitySwitches(string headlessSwitch)
    {
        var commandLine = Encoding.UTF8.GetBytes(
            $"/opt/Unity/Hub/Editor/6000.0.64f1/Editor/Unity\0{headlessSwitch}\0");

        var process = LinuxPlatform.TryParseProcess(
            5252,
            "/opt/Unity/Hub/Editor/6000.0.64f1/Editor/Unity",
            commandLine);

        Assert.NotNull(process);
        Assert.True(process.IsBatchMode);
        Assert.False(process.HasMainWindow);
        Assert.False(process.IsInteractiveEditor);
    }

    [Fact]
    public void TryParseProcess_RejectsUnrelatedUnityNamedExecutable()
    {
        var process = LinuxPlatform.TryParseProcess(
            6262,
            "/usr/local/bin/Unity",
            "/usr/local/bin/Unity\0"u8);

        Assert.Null(process);
    }

    [Fact]
    public async Task FindRunningUnityProcesses_ReadsProcCmdline()
    {
        if (!OperatingSystem.IsLinux())
            return;

        using var tempDirectory = new TemporaryDirectory();
        var editorDirectory = Path.Combine(
            tempDirectory.Path,
            "6000.3.16f1",
            "Editor");
        Directory.CreateDirectory(editorDirectory);
        var executable = Path.Combine(editorDirectory, "Unity");
        File.Copy("/bin/sh", executable);
        File.SetUnixFileMode(
            executable,
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

        using var probe = Process.Start(new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            ArgumentList =
            {
                "-c",
                "sleep 30",
                "unityctl-probe",
                "-projectPath",
                tempDirectory.Path
            }
        });
        Assert.NotNull(probe);

        try
        {
            UnityProcessInfo? detected = null;
            for (var attempt = 0; attempt < 20 && detected == null; attempt++)
            {
                detected = new LinuxPlatform()
                    .FindRunningUnityProcesses()
                    .SingleOrDefault(process => process.ProcessId == probe.Id);
                if (detected == null)
                    await Task.Delay(25);
            }

            Assert.NotNull(detected);
            Assert.Equal(tempDirectory.Path, detected.ProjectPath);
            Assert.Equal("6000.3.16f1", detected.Version);
            Assert.True(detected.IsInteractiveEditor);
        }
        finally
        {
            if (!probe.HasExited)
                probe.Kill(entireProcessTree: true);
        }
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
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"unityctl-tests-{Guid.NewGuid():N}");
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
