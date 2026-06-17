using Unityctl.Cli.Commands;
using Xunit;

namespace Unityctl.Cli.Tests;

public class StatusCommandTests
{
    [Fact]
    public async Task SmartWait_IpcReadyImmediately_ReturnsWithoutDelay()
    {
        var probeCount = 0;
        var result = await StatusCommand.ExecuteWithSmartWaitAsync(
            @"C:\project",
            json: true,
            isProjectLocked: _ => true,
            probeIpcAsync: (_, _) =>
            {
                probeCount++;
                return Task.FromResult(true); // IPC ready on first try
            },
            isInteractiveEditorRunning: _ => true,
            delayAsync: (_, _) => Task.CompletedTask);

        // Should have probed exactly once (immediate success)
        Assert.Equal(1, probeCount);
    }

    [Fact]
    public async Task SmartWait_UnlockedProject_FallsThroughImmediately()
    {
        var probeCount = 0;
        var result = await StatusCommand.ExecuteWithSmartWaitAsync(
            @"C:\project",
            json: true,
            isProjectLocked: _ => false, // Not locked = Unity not running
            probeIpcAsync: (_, _) =>
            {
                probeCount++;
                return Task.FromResult(false);
            },
            isInteractiveEditorRunning: _ => true,
            delayAsync: (_, _) => Task.CompletedTask);

        // Should probe once, see unlocked, and fall through
        Assert.Equal(1, probeCount);
    }

    [Fact]
    public async Task SmartWait_LockedThenIpcReady_WaitsAndSucceeds()
    {
        var probeCount = 0;
        var result = await StatusCommand.ExecuteWithSmartWaitAsync(
            @"C:\project",
            json: true,
            isProjectLocked: _ => true,
            probeIpcAsync: (_, _) =>
            {
                probeCount++;
                return Task.FromResult(probeCount >= 3); // Ready on 3rd attempt
            },
            isInteractiveEditorRunning: _ => true,
            delayAsync: (_, _) => Task.CompletedTask);

        Assert.Equal(3, probeCount);
    }

    [Fact]
    public async Task SmartWait_LockedThenUnlocked_StopsEarly()
    {
        var probeCount = 0;
        var result = await StatusCommand.ExecuteWithSmartWaitAsync(
            @"C:\project",
            json: true,
            isProjectLocked: _ => probeCount < 2, // Unlocks after 2 probes
            probeIpcAsync: (_, _) =>
            {
                probeCount++;
                return Task.FromResult(false);
            },
            isInteractiveEditorRunning: _ => true,
            delayAsync: (_, _) => Task.CompletedTask);

        // Should stop after lockfile disappears (2 probes)
        Assert.Equal(2, probeCount);
    }
}
