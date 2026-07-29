using Unityctl.Core.Transport;
using Unityctl.Core.Discovery;
using Unityctl.Core.Platform;
using Unityctl.Shared.Models;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Core.Tests.Transport;

public sealed class CommandExecutorReadinessTests
{
    [Fact]
    public async Task ExecuteAsync_LockedByHeadlessProcess_ReturnsBusyWithoutBatchFallback()
    {
        var projectPath = Path.Combine(Path.GetTempPath(), $"unityctl-headless-executor-{Guid.NewGuid():N}");
        Directory.CreateDirectory(projectPath);
        try
        {
            var platform = new FakePlatform(
                locked: true,
                new UnityProcessInfo
                {
                    ProcessId = 6681,
                    ProjectPath = projectPath,
                    IsBatchMode = true
                });
            var executor = new CommandExecutor(platform, new UnityEditorDiscovery(platform));

            var response = await executor.ExecuteAsync(
                projectPath,
                new CommandRequest { Command = WellKnownCommands.Check });

            Assert.False(response.Success);
            Assert.Equal(StatusCode.Busy, response.StatusCode);
            Assert.Contains("headless Unity process", response.Message);
            Assert.Equal("check", response.Data!["command"]!.GetValue<string>());
            Assert.Equal("headless-process-holding-lock", response.Data["target"]!["fallbackReason"]!.GetValue<string>());
            Assert.Equal("headless", response.Data["target"]!["processKind"]!.GetValue<string>());
            Assert.Equal(6681, response.Data["target"]!["unityPid"]!.GetValue<int>());
            Assert.Null(response.Data["target"]!["transport"]);
        }
        finally
        {
            if (Directory.Exists(projectPath))
                Directory.Delete(projectPath, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_LockedByInteractiveEditor_ReturnsBusyWithoutBatchFallback()
    {
        var projectPath = Path.Combine(Path.GetTempPath(), $"unityctl-interactive-executor-{Guid.NewGuid():N}");
        Directory.CreateDirectory(projectPath);
        try
        {
            var platform = new FakePlatform(
                locked: true,
                new UnityProcessInfo
                {
                    ProcessId = 6682,
                    ProjectPath = projectPath,
                    HasMainWindow = true
                });
            var executor = new CommandExecutor(platform, new UnityEditorDiscovery(platform));

            var response = await executor.ExecuteAsync(
                projectPath,
                new CommandRequest { Command = WellKnownCommands.Check });

            Assert.False(response.Success);
            Assert.Equal(StatusCode.Busy, response.StatusCode);
            Assert.Contains("IPC is not ready", response.Message);
            Assert.Equal("editor-running-ipc-not-ready", response.Data!["target"]!["fallbackReason"]!.GetValue<string>());
            Assert.Equal("interactive", response.Data["target"]!["processKind"]!.GetValue<string>());
            Assert.Equal(6682, response.Data["target"]!["unityPid"]!.GetValue<int>());
            Assert.True(response.Data["target"]!["projectLocked"]!.GetValue<bool>());
            Assert.Null(response.Data["target"]!["transport"]);
        }
        finally
        {
            if (Directory.Exists(projectPath))
                Directory.Delete(projectPath, recursive: true);
        }
    }

    [Fact]
    public void BuildInteractiveBusyResponse_ForScriptGetErrors_AddsScriptSpecificGuidance()
    {
        var response = CommandExecutor.BuildInteractiveBusyResponse(
            @"C:\Users\gmdqn\robotapp",
            WellKnownCommands.ScriptGetErrors);

        Assert.Equal(StatusCode.Busy, response.StatusCode);
        Assert.Contains("script get-errors", response.Message);
        Assert.True(response.Data!["requiresIpcReady"]!.GetValue<bool>());
        Assert.Contains("script validate", response.Data["followUpAction"]!.GetValue<string>());
    }

    [Fact]
    public void BuildInteractiveBusyResponse_ForNonScriptCommand_KeepsGenericMessage()
    {
        var response = CommandExecutor.BuildInteractiveBusyResponse(
            @"C:\Users\gmdqn\robotapp",
            WellKnownCommands.Status);

        Assert.Equal(StatusCode.Busy, response.StatusCode);
        Assert.Contains("IPC is not ready", response.Message);
        Assert.Null(response.Data);
    }

    [Fact]
    public void BuildInteractiveBusyResponse_ForUiInput_AddsUiSpecificGuidance()
    {
        var response = CommandExecutor.BuildInteractiveBusyResponse(
            @"C:\Users\gmdqn\robotapp",
            WellKnownCommands.UiInput);

        Assert.Equal(StatusCode.Busy, response.StatusCode);
        Assert.Contains("ui input", response.Message);
        Assert.True(response.Data!["requiresIpcReady"]!.GetValue<bool>());
        Assert.Contains("deterministically", response.Data["followUpAction"]!.GetValue<string>());
    }

    [Fact]
    public void BuildInteractiveBusyResponse_ForUiClick_AddsPlayModeGuidance()
    {
        var response = CommandExecutor.BuildInteractiveBusyResponse(
            @"C:\Users\gmdqn\robotapp",
            WellKnownCommands.UiClick);

        Assert.Equal(StatusCode.Busy, response.StatusCode);
        Assert.Contains("ui click", response.Message);
        Assert.Contains("Button.onClick", response.Data!["followUpAction"]!.GetValue<string>());
    }

    [Fact]
    public void AttachTargetMetadata_AddsNormalizedTargetBlock()
    {
        var response = CommandExecutor.AttachTargetMetadata(
            CommandResponse.Ok("ok"),
            @"C:\Users\gmdqn\RobotApp",
            "ipc",
            new UnityEditorInfo
            {
                Version = "6000.0.64f1",
                Location = @"C:\Program Files\Unity\Hub\Editor\6000.0.64f1"
            },
            new UnityProcessInfo
            {
                ProcessId = 2944,
                ProjectPath = @"C:\Users\gmdqn\RobotApp"
            },
            projectLocked: true,
            fallbackReason: "ipc-probe-failed");

        Assert.NotNull(response.Data);
        Assert.Equal("ipc", response.Data!["target"]!["transport"]!.GetValue<string>());
        Assert.Equal("6000.0.64f1", response.Data["target"]!["editorVersion"]!.GetValue<string>());
        Assert.Contains("unityctl_", response.Data["target"]!["pipeName"]!.GetValue<string>());
        Assert.Equal(2944, response.Data["target"]!["unityPid"]!.GetValue<int>());
        Assert.True(response.Data["target"]!["projectLocked"]!.GetValue<bool>());
        Assert.Equal("ipc-probe-failed", response.Data["target"]!["fallbackReason"]!.GetValue<string>());
    }

    [Fact]
    public void BuildHeadlessBusyResponse_ExplainsInteractiveRequirement()
    {
        var response = CommandExecutor.BuildHeadlessBusyResponse(
            WellKnownCommands.Status,
            new UnityProcessInfo
            {
                ProcessId = 40184,
                IsBatchMode = true
            });

        Assert.Equal(StatusCode.Busy, response.StatusCode);
        Assert.Contains("headless Unity process", response.Message);
        Assert.True(response.Data!["requiresInteractiveEditor"]!.GetValue<bool>());
    }

    [Fact]
    public void BuildHeadlessBusyResponse_ForProjectValidate_UsesCliCommandName()
    {
        var response = CommandExecutor.BuildHeadlessBusyResponse(
            WellKnownCommands.ProjectValidate,
            new UnityProcessInfo
            {
                ProcessId = 40185,
                IsBatchMode = true
            });

        Assert.Equal(StatusCode.Busy, response.StatusCode);
        Assert.Contains("project validate", response.Message);
        Assert.Equal("project validate", response.Data!["command"]!.GetValue<string>());
    }

    [Fact]
    public async Task ExecuteAsync_FindRefsUsesLocalScannerEvenWhenEditorIsLocked()
    {
        var projectPath = Path.Combine(Path.GetTempPath(), $"unityctl-local-refs-{Guid.NewGuid():N}");
        var assetsPath = Path.Combine(projectPath, "Assets");
        Directory.CreateDirectory(assetsPath);
        File.WriteAllText(Path.Combine(assetsPath, "Probe.cs"), "target target\n");
        try
        {
            var platform = new FakePlatform(locked: true);
            var executor = new CommandExecutor(platform, new UnityEditorDiscovery(platform));
            var request = new CommandRequest
            {
                Command = WellKnownCommands.ScriptFindRefs,
                Parameters = new() { ["symbol"] = "target" }
            };

            var response = await executor.ExecuteAsync(projectPath, request);

            Assert.True(response.Success);
            Assert.Equal(2, response.Data!["referenceCount"]!.GetValue<int>());
            Assert.Equal("local", response.Data["target"]!["transport"]!.GetValue<string>());
            Assert.False(response.Data["target"]!["requiresEditor"]!.GetValue<bool>());
        }
        finally
        {
            Directory.Delete(projectPath, recursive: true);
        }
    }

    private sealed class FakePlatform : IPlatformServices
    {
        private readonly bool _locked;
        private readonly IReadOnlyList<UnityProcessInfo> _processes;

        public FakePlatform(bool locked, params UnityProcessInfo[] processes)
        {
            _locked = locked;
            _processes = processes;
        }

        public string GetUnityHubEditorsJsonPath() => Path.Combine(Path.GetTempPath(), "missing-editors.json");

        public IEnumerable<string> GetDefaultEditorSearchPaths() => [];

        public string GetUnityExecutablePath(string editorBasePath) => Path.Combine(editorBasePath, "Unity");

        public IEnumerable<UnityProcessInfo> FindRunningUnityProcesses() => _processes;

        public bool IsProjectLocked(string projectPath) => _locked;

        public Stream CreateIpcClientStream(string projectPath) => throw new NotSupportedException();

        public string GetTempResponseFilePath()
            => Path.Combine(Path.GetTempPath(), $"unityctl-command-executor-{Guid.NewGuid():N}.json");
    }
}
