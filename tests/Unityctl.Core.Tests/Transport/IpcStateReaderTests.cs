#nullable enable

using System.Text.Json;
using Unityctl.Core.Transport;
using Unityctl.Shared;
using Unityctl.Shared.Models;
using Unityctl.Shared.Serialization;
using Xunit;

namespace Unityctl.Core.Tests.Transport;

public sealed class IpcStateReaderTests : IDisposable
{
    private readonly string _tempProjectPath;

    public IpcStateReaderTests()
    {
        _tempProjectPath = Path.Combine(Path.GetTempPath(), $"unityctl-state-reader-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempProjectPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempProjectPath))
            Directory.Delete(_tempProjectPath, recursive: true);
    }

    [Fact]
    public void Read_FileMissing_ReturnsNull()
    {
        var reader = new IpcStateReader();
        var result = reader.Read(_tempProjectPath);
        Assert.Null(result);
    }

    [Fact]
    public void Read_ValidFile_ReturnsState()
    {
        var reader = new IpcStateReader();
        var now = DateTime.UtcNow;
        var state = new IpcState
        {
            PipeName = "unityctl_abcd1234",
            Pid = 12345,
            UnityVersion = "2022.3.0f1",
            State = IpcStateValues.Ready,
            UpdatedAtUtc = now
        };

        WriteStateFile(state);

        var result = reader.Read(_tempProjectPath);
        Assert.NotNull(result);
        Assert.Equal("unityctl_abcd1234", result.PipeName);
        Assert.Equal(12345, result.Pid);
        Assert.Equal("2022.3.0f1", result.UnityVersion);
        Assert.Equal(IpcStateValues.Ready, result.State);
        Assert.Equal(now, result.UpdatedAtUtc);
    }

    [Fact]
    public void Read_MalformedJson_ReturnsNull()
    {
        var stateDir = Path.Combine(_tempProjectPath, "Library", "Unityctl");
        Directory.CreateDirectory(stateDir);
        var filePath = Path.Combine(stateDir, "ipc-state.json");
        File.WriteAllText(filePath, "{invalid json}");

        var reader = new IpcStateReader();
        var result = reader.Read(_tempProjectPath);
        Assert.Null(result);
    }

    [Fact]
    public void Read_UnreadableFile_ReturnsNull()
    {
        var reader = new IpcStateReader();
        // Use a path with invalid characters to trigger read failure
        var badPath = Path.Combine(_tempProjectPath, "\x00invalid");
        var result = reader.Read(badPath);
        Assert.Null(result);
    }

    [Fact]
    public void IsFresh_WithinStalenessWindow_ReturnsTrue()
    {
        var now = DateTime.UtcNow;
        var state = new IpcState { UpdatedAtUtc = now };

        // Should be fresh (age is ~0ms)
        Assert.True(state.IsFresh(Constants.IpcStateStalenessMs));
    }

    [Fact]
    public void IsFresh_BeyondStalenessWindow_ReturnsFalse()
    {
        var state = new IpcState
        {
            UpdatedAtUtc = DateTime.UtcNow.AddSeconds(-20) // 20 seconds old, beyond 15s stale window
        };

        Assert.False(state.IsFresh(Constants.IpcStateStalenessMs));
    }

    [Fact]
    public void IsReloadingFresh_ReloadingAndFresh_ReturnsTrue()
    {
        var state = new IpcState
        {
            State = IpcStateValues.Reloading,
            UpdatedAtUtc = DateTime.UtcNow
        };

        Assert.True(state.IsReloadingFresh());
    }

    [Fact]
    public void IsReloadingFresh_ReadyNotReloading_ReturnsFalse()
    {
        var state = new IpcState
        {
            State = IpcStateValues.Ready,
            UpdatedAtUtc = DateTime.UtcNow
        };

        Assert.False(state.IsReloadingFresh());
    }

    [Fact]
    public void IsReloadingFresh_ReloadingButStale_ReturnsFalse()
    {
        var state = new IpcState
        {
            State = IpcStateValues.Reloading,
            UpdatedAtUtc = DateTime.UtcNow.AddSeconds(-20) // Beyond 15s stale window
        };

        Assert.False(state.IsReloadingFresh());
    }

    [Fact]
    public void IsStartingFresh_StartingAndFresh_ReturnsTrue()
    {
        var state = new IpcState
        {
            State = IpcStateValues.Starting,
            UpdatedAtUtc = DateTime.UtcNow
        };

        Assert.True(state.IsStartingFresh());
    }

    [Fact]
    public void IsStoppedFresh_StoppedAndFresh_ReturnsTrue()
    {
        var state = new IpcState
        {
            State = IpcStateValues.Stopped,
            UpdatedAtUtc = DateTime.UtcNow
        };

        Assert.True(state.IsStoppedFresh());
    }

    private void WriteStateFile(IpcState state)
    {
        var stateDir = Path.Combine(_tempProjectPath, "Library", "Unityctl");
        Directory.CreateDirectory(stateDir);
        var filePath = Path.Combine(stateDir, "ipc-state.json");
        var json = JsonSerializer.Serialize(state, UnityctlJsonContext.Default.IpcState);
        File.WriteAllText(filePath, json);
    }
}
