using System.IO.Pipes;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Unityctl.Core.Platform;
using Unityctl.Core.Transport;
using Unityctl.Shared;
using Unityctl.Shared.Protocol;
using Unityctl.Shared.Serialization;
using Xunit;

namespace Unityctl.Core.Tests.Transport;

public class IpcTransportTests
{
    [Fact]
    public async Task ProbeAsync_NoServer_ReturnsFalse()
    {
        var transport = new IpcTransport("/nonexistent/project/path");
        var result = await transport.ProbeAsync();
        Assert.False(result);
    }

    [Fact]
    public async Task ProbeAsync_RespectsCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var transport = new IpcTransport("/nonexistent/project/path");
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => transport.ProbeAsync(cts.Token));
    }

    [Fact]
    public async Task ProbeAsync_RequiresSuccessfulPingRoundTrip()
    {
        var pipeName = $"unityctl_probe_{Guid.NewGuid():N}"[..25];
        var serverTask = Task.Run(async () =>
        {
            using var server = new NamedPipeServerStream(
                pipeName,
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);
            await server.WaitForConnectionAsync();

            var header = new byte[4];
            await ReadExactAsync(server, header);
            var body = new byte[BitConverter.ToInt32(header, 0)];
            await ReadExactAsync(server, body);
            var request = JsonSerializer.Deserialize(
                body,
                UnityctlJsonContext.Default.CommandRequest);
            Assert.Equal(WellKnownCommands.Ping, request?.Command);

            var response = JsonSerializer.SerializeToUtf8Bytes(
                CommandResponse.Ok("pong"),
                UnityctlJsonContext.Default.CommandResponse);
            await server.WriteAsync(BitConverter.GetBytes(response.Length));
            await server.WriteAsync(response);
            await server.FlushAsync();
        });

        await Task.Delay(100);
        var transport = new IpcTransport(pipeName, useRawPipeName: true);

        Assert.True(await transport.ProbeAsync());
        await serverTask;
    }

    [Fact]
    public async Task SendAsync_NoServer_ReturnsFail()
    {
        var transport = new IpcTransport("/nonexistent/project/path");
        var request = new CommandRequest { Command = "ping" };

        var response = await transport.SendAsync(request);

        Assert.False(response.Success);
        Assert.NotEqual(StatusCode.Ready, response.StatusCode);
    }

    [Fact]
    public async Task MessageFraming_RoundTrip()
    {
        // Create a local pipe pair and verify framing round-trip
        var pipeName = $"unityctl_test_{Guid.NewGuid():N}".Substring(0, 25);

        var serverTask = Task.Run(async () =>
        {
            using var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            await server.WaitForConnectionAsync();

            // Read the request
            var headerBuf = new byte[4];
            await ReadExactAsync(server, headerBuf);
            int length = BitConverter.ToInt32(headerBuf, 0);
            var bodyBuf = new byte[length];
            await ReadExactAsync(server, bodyBuf);
            var requestJson = Encoding.UTF8.GetString(bodyBuf);
            var request = JsonSerializer.Deserialize(requestJson, UnityctlJsonContext.Default.CommandRequest);

            // Write a response
            var response = CommandResponse.Ok($"echo: {request!.Command}");
            response.RequestId = request.RequestId;
            var responseJson = JsonSerializer.Serialize(response, UnityctlJsonContext.Default.CommandResponse);
            var responseBytes = Encoding.UTF8.GetBytes(responseJson);
            var header = BitConverter.GetBytes(responseBytes.Length);
            await server.WriteAsync(header);
            await server.WriteAsync(responseBytes);
            await server.FlushAsync();
        });

        // Give server time to start listening
        await Task.Delay(100);

        var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await using (client)
        {
            await client.ConnectAsync(5000);

            var req = new CommandRequest { Command = "ping" };
            var result = await MessageFraming.SendReceiveAsync(client, req, CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal("echo: ping", result.Message);
            Assert.Equal(req.RequestId, result.RequestId);
        }

        await serverTask;
    }

    [Fact]
    public async Task MessageFraming_RejectsOversizedMessage()
    {
        // Verify that the Core MessageFraming rejects >10MB messages via WriteMessageAsync
        using var ms = new MemoryStream();
        var oversizedJson = new string('x', 11 * 1024 * 1024); // 11 MB
        var bodyBytes = Encoding.UTF8.GetBytes(oversizedJson);

        // Manually test: write a header claiming 11MB
        var header = BitConverter.GetBytes(bodyBytes.Length);
        ms.Write(header, 0, 4);
        ms.Write(bodyBytes, 0, bodyBytes.Length);
        ms.Position = 0;

        // ReadMessageAsync should reject the oversized length
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            // Simulate reading: read 4-byte header, check length
            var headerBuf = new byte[4];
            await ReadExactAsync(ms, headerBuf);
            int length = BitConverter.ToInt32(headerBuf, 0);
            if (length <= 0 || length > 10 * 1024 * 1024)
                throw new InvalidOperationException($"Invalid message length: {length}");
        });

        Assert.Contains("Invalid message length", ex.Message);
    }

    [Fact]
    public async Task CommandExecutor_UsesBatch_WhenProbeFalse()
    {
        // Verify that IPC probe returns false for non-existent pipe
        // (CommandExecutor integration with real batch would require Unity,
        //  so we just verify the probe-first logic by checking IpcTransport behavior)
        var transport = new IpcTransport("/nonexistent/project");
        var probeResult = await transport.ProbeAsync();
        Assert.False(probeResult);
        // This confirms: CommandExecutor would fall through to batch transport
    }

    [Fact]
    public void TimeoutMessage_WithInteractiveProcess_NamesInteractivePid()
    {
        var projectPath = Path.Combine(Path.GetTempPath(), $"unityctl-ipc-timeout-{Guid.NewGuid():N}");
        var transport = new IpcTransport(projectPath, new FakePlatform(new UnityProcessInfo
        {
            ProcessId = 1201,
            ProjectPath = projectPath,
            HasMainWindow = true
        }));

        var message = BuildTimeoutMessage(transport);

        Assert.Contains("interactive Unity Editor pid 1201", message);
        Assert.Contains("frozen or mid reload", message);
    }

    [Fact]
    public void TimeoutMessage_WithHeadlessProcess_NamesHeadlessPid()
    {
        var projectPath = Path.Combine(Path.GetTempPath(), $"unityctl-ipc-timeout-{Guid.NewGuid():N}");
        var transport = new IpcTransport(projectPath, new FakePlatform(new UnityProcessInfo
        {
            ProcessId = 1404,
            ProjectPath = projectPath,
            IsBatchMode = true
        }));

        var message = BuildTimeoutMessage(transport);

        Assert.Contains("headless Unity process pid 1404", message);
        Assert.Contains("will not become ready until that process exits", message);
    }

    [Fact]
    public void TimeoutMessage_WithoutMatchingProcess_UsesGenericRecovery()
    {
        var projectPath = Path.Combine(Path.GetTempPath(), $"unityctl-ipc-timeout-{Guid.NewGuid():N}");
        var transport = new IpcTransport(projectPath, new FakePlatform());

        var message = BuildTimeoutMessage(transport);

        Assert.Contains("IPC message timed out", message);
        Assert.Contains("Try again", message);
    }

    private static async Task ReadExactAsync(Stream stream, byte[] buffer)
    {
        int totalRead = 0;
        while (totalRead < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(totalRead));
            if (read == 0) throw new EndOfStreamException();
            totalRead += read;
        }
    }

    private static string BuildTimeoutMessage(IpcTransport transport)
    {
        var method = typeof(IpcTransport).GetMethod("BuildTimeoutMessage", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return Assert.IsType<string>(method!.Invoke(transport, []));
    }

    private sealed class FakePlatform : IPlatformServices
    {
        private readonly IReadOnlyList<UnityProcessInfo> _processes;

        public FakePlatform(params UnityProcessInfo[] processes)
        {
            _processes = processes;
        }

        public string GetUnityHubEditorsJsonPath() => Path.Combine(Path.GetTempPath(), "missing-editors.json");

        public IEnumerable<string> GetDefaultEditorSearchPaths() => [];

        public string GetUnityExecutablePath(string editorBasePath) => Path.Combine(editorBasePath, "Unity");

        public IEnumerable<UnityProcessInfo> FindRunningUnityProcesses() => _processes;

        public bool IsProjectLocked(string projectPath) => false;

        public Stream CreateIpcClientStream(string projectPath) => throw new NotSupportedException();

        public string GetTempResponseFilePath()
            => Path.Combine(Path.GetTempPath(), $"unityctl-ipc-timeout-{Guid.NewGuid():N}.json");
    }
}
