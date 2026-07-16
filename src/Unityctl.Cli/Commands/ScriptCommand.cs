using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Core.Discovery;
using Unityctl.Core.Platform;
using Unityctl.Core.Transport;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class ScriptCommand
{
    private const int InteractiveScriptProbeAttempts = 12;
    private const int InteractiveScriptProbeDelayMs = 1000;

    public static void Create(string project, string path, string className, string? ns = null, string baseType = "MonoBehaviour", string? content = null, string? contentFile = null, bool json = false)
    {
        // CLI-side validation: filename must match className
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(path);
        if (!string.Equals(fileNameWithoutExt, className, StringComparison.Ordinal))
        {
            Console.Error.WriteLine($"Error: filename '{fileNameWithoutExt}' does not match className '{className}'");
            Environment.Exit(1);
            return;
        }

        // Optional initial content: --content-file reads a file, --content is inline.
        // When neither is given, the plugin generates a template (existing behavior).
        if (content != null && contentFile != null)
        {
            Console.Error.WriteLine("Error: --content and --content-file are mutually exclusive");
            Environment.Exit(1);
            return;
        }
        if (contentFile != null)
        {
            if (!File.Exists(contentFile))
            {
                Console.Error.WriteLine($"Error: content file not found: {contentFile}");
                Environment.Exit(1);
                return;
            }
            content = File.ReadAllText(contentFile);
        }
        if (content != null && content.Length > 9 * 1024 * 1024)
        {
            Console.Error.WriteLine("Error: content exceeds maximum size (9MB)");
            Environment.Exit(1);
            return;
        }

        var request = CreateCreateRequest(path, className, ns, baseType, content);
        CommandRunner.Execute(project, request, json);
    }

    public static void Edit(string project, string path, string? content = null, string? contentFile = null, bool json = false)
    {
        // Exactly one of content or contentFile
        if (content == null && contentFile == null)
        {
            Console.Error.WriteLine("Error: exactly one of --content or --content-file is required");
            Environment.Exit(1);
            return;
        }
        if (content != null && contentFile != null)
        {
            Console.Error.WriteLine("Error: --content and --content-file are mutually exclusive");
            Environment.Exit(1);
            return;
        }

        // CLI reads contentFile and sends as content
        if (contentFile != null)
        {
            if (!File.Exists(contentFile))
            {
                Console.Error.WriteLine($"Error: content file not found: {contentFile}");
                Environment.Exit(1);
                return;
            }
            content = File.ReadAllText(contentFile);
        }

        // Check IPC 10MB limit
        if (content!.Length > 9 * 1024 * 1024) // leave margin for JSON envelope
        {
            Console.Error.WriteLine("Error: content exceeds maximum size (9MB)");
            Environment.Exit(1);
            return;
        }

        var request = CreateEditRequest(path, content);
        CommandRunner.Execute(project, request, json);
    }

    public static void Delete(string project, string path, bool json = false)
    {
        var request = CreateDeleteRequest(path);
        CommandRunner.Execute(project, request, json);
    }

    public static void Validate(string project, string? path = null, bool wait = true, int timeout = 300, bool json = false)
    {
        var exitCode = ValidateAsync(project, path, wait, timeout, json).GetAwaiter().GetResult();
        Environment.Exit(exitCode);
    }

    internal static async Task<int> ValidateAsync(
        string project,
        string? path,
        bool wait,
        int timeout,
        bool json)
    {
        var request = CreateValidateRequest(path);

        var platform = PlatformFactory.Create();
        var discovery = new UnityEditorDiscovery(platform);
        var executor = new CommandExecutor(platform, discovery);

        CommandResponse response;

        if (wait)
        {
            response = await AsyncCommandRunner.ExecuteAsync(
                project,
                request,
                async (proj, req, ct) => await executor.ExecuteAsync(proj, req, ct: ct),
                pollCommand: WellKnownCommands.ScriptValidateResult,
                timeoutSeconds: timeout,
                timeoutStatusCode: StatusCode.BuildFailed,
                timeoutMessage: $"Script validation timed out after {timeout}s");
        }
        else
        {
            response = await executor.ExecuteAsync(project, request);
        }

        CommandRunner.PrintResponse(response, json);
        return CommandRunner.GetExitCode(response);
    }

    public static void List(string project, string? folder = null, string? filter = null, int? limit = null, bool json = false)
    {
        var request = CreateListRequest(folder, filter, limit);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateListRequest(string? folder = null, string? filter = null, int? limit = null)
    {
        var parameters = new JsonObject();
        if (!string.IsNullOrWhiteSpace(folder)) parameters["folder"] = folder;
        if (!string.IsNullOrWhiteSpace(filter)) parameters["filter"] = filter;
        if (limit.HasValue) parameters["limit"] = limit.Value;

        return new CommandRequest
        {
            Command = WellKnownCommands.ScriptList,
            Parameters = parameters
        };
    }

    internal static CommandRequest CreateCreateRequest(string path, string className, string? ns, string baseType, string? content = null)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path must not be empty", nameof(path));
        if (string.IsNullOrWhiteSpace(className))
            throw new ArgumentException("className must not be empty", nameof(className));

        var parameters = new JsonObject
        {
            ["path"] = path,
            ["className"] = className,
            ["baseType"] = baseType
        };
        if (!string.IsNullOrEmpty(ns)) parameters["namespace"] = ns;
        if (!string.IsNullOrEmpty(content)) parameters["content"] = content;

        return new CommandRequest
        {
            Command = WellKnownCommands.ScriptCreate,
            Parameters = parameters
        };
    }

    internal static CommandRequest CreateEditRequest(string path, string content)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path must not be empty", nameof(path));
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        return new CommandRequest
        {
            Command = WellKnownCommands.ScriptEdit,
            Parameters = new JsonObject
            {
                ["path"] = path,
                ["content"] = content
            }
        };
    }

    public static void Patch(string project, string path, int startLine, int deleteCount = 0, string? insertContent = null, string? insertContentFile = null, bool json = false)
    {
        if (insertContent != null && insertContentFile != null)
        {
            Console.Error.WriteLine("Error: --insert-content and --insert-content-file are mutually exclusive");
            Environment.Exit(1);
            return;
        }

        if (insertContentFile != null)
        {
            if (!File.Exists(insertContentFile))
            {
                Console.Error.WriteLine($"Error: insert content file not found: {insertContentFile}");
                Environment.Exit(1);
                return;
            }
            insertContent = File.ReadAllText(insertContentFile);
        }

        // CLI: unescape literal \n to real newlines for ergonomic multi-line input
        if (insertContent != null)
            insertContent = insertContent.Replace("\\n", "\n");

        var request = CreatePatchRequest(path, startLine, deleteCount, insertContent);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreatePatchRequest(string path, int startLine, int deleteCount, string? insertContent)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path must not be empty", nameof(path));

        var parameters = new JsonObject
        {
            ["path"] = path,
            ["startLine"] = startLine,
            ["deleteCount"] = deleteCount
        };
        if (insertContent != null) parameters["insertContent"] = insertContent;

        return new CommandRequest
        {
            Command = WellKnownCommands.ScriptPatch,
            Parameters = parameters
        };
    }

    internal static CommandRequest CreateDeleteRequest(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path must not be empty", nameof(path));

        return new CommandRequest
        {
            Command = WellKnownCommands.ScriptDelete,
            Parameters = new JsonObject { ["path"] = path }
        };
    }

    internal static CommandRequest CreateValidateRequest(string? path)
    {
        var parameters = new JsonObject();
        if (!string.IsNullOrEmpty(path)) parameters["path"] = path;

        return new CommandRequest
        {
            Command = WellKnownCommands.ScriptValidate,
            Parameters = parameters
        };
    }

    public static void GetErrors(string project, string? path = null, bool json = false)
    {
        var request = CreateGetErrorsRequest(path);
        var exitCode = ExecuteInteractiveAsync(project, request, json).GetAwaiter().GetResult();
        Environment.Exit(exitCode);
    }

    internal static CommandRequest CreateGetErrorsRequest(string? path = null)
    {
        var parameters = new JsonObject();
        if (!string.IsNullOrWhiteSpace(path)) parameters["path"] = path;

        return new CommandRequest
        {
            Command = WellKnownCommands.ScriptGetErrors,
            Parameters = parameters
        };
    }

    public static void FindRefs(string project, string symbol, string? folder = null, int? limit = null, bool json = false)
    {
        var request = CreateFindRefsRequest(symbol, folder, limit);
        var exitCode = ExecuteInteractiveAsync(project, request, json).GetAwaiter().GetResult();
        Environment.Exit(exitCode);
    }

    internal static CommandRequest CreateFindRefsRequest(string symbol, string? folder = null, int? limit = null)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("symbol must not be empty", nameof(symbol));

        var parameters = new JsonObject { ["symbol"] = symbol };
        if (!string.IsNullOrWhiteSpace(folder)) parameters["folder"] = folder;
        if (limit.HasValue) parameters["limit"] = limit.Value;

        return new CommandRequest
        {
            Command = WellKnownCommands.ScriptFindRefs,
            Parameters = parameters
        };
    }

    public static void RenameSymbol(string project, string oldName, string newName, string? folder = null, bool dryRun = false, bool json = false)
    {
        var request = CreateRenameSymbolRequest(oldName, newName, folder, dryRun);
        var exitCode = ExecuteInteractiveAsync(project, request, json).GetAwaiter().GetResult();
        Environment.Exit(exitCode);
    }

    internal static CommandRequest CreateRenameSymbolRequest(string oldName, string newName, string? folder = null, bool dryRun = false)
    {
        if (string.IsNullOrWhiteSpace(oldName))
            throw new ArgumentException("oldName must not be empty", nameof(oldName));
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("newName must not be empty", nameof(newName));

        var parameters = new JsonObject
        {
            ["oldName"] = oldName,
            ["newName"] = newName
        };
        if (!string.IsNullOrWhiteSpace(folder)) parameters["folder"] = folder;
        if (dryRun) parameters["dryRun"] = true;

        return new CommandRequest
        {
            Command = WellKnownCommands.ScriptRenameSymbol,
            Parameters = parameters
        };
    }

    internal static async Task<int> ExecuteInteractiveAsync(
        string project,
        CommandRequest request,
        bool json,
        Func<string, bool>? isProjectLocked = null,
        Func<string, CancellationToken, Task<bool>>? probeIpcAsync = null,
        Func<string, CommandRequest, bool, bool, Task<int>>? executeAsync = null,
        CancellationToken ct = default)
    {
        var readiness = await EnsureInteractiveEditorReadyAsync(
            project,
            isProjectLocked ?? (path => PlatformFactory.Create().IsProjectLocked(path)),
            probeIpcAsync ?? ProbeIpcAsync,
            InteractiveScriptProbeAttempts,
            InteractiveScriptProbeDelayMs,
            ct);

        if (readiness == ScriptInteractiveReadinessResult.TimedOut)
        {
            var response = CreateInteractiveReadinessFailureResponse(project, request.Command);
            CommandRunner.PrintResponse(project, response, json);
            return CommandRunner.GetExitCode(response);
        }

        return await (executeAsync ?? CommandRunner.ExecuteAsync)(project, request, json, false);
    }

    internal static async Task<ScriptInteractiveReadinessResult> EnsureInteractiveEditorReadyAsync(
        string project,
        Func<string, bool> isProjectLocked,
        Func<string, CancellationToken, Task<bool>> probeIpcAsync,
        int maxAttempts,
        int delayMs,
        CancellationToken ct = default)
    {
        if (!isProjectLocked(project))
            return ScriptInteractiveReadinessResult.ContinueWithoutReadyIpc;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            if (await probeIpcAsync(project, ct).ConfigureAwait(false))
                return ScriptInteractiveReadinessResult.Ready;

            if (!isProjectLocked(project))
                return ScriptInteractiveReadinessResult.ContinueWithoutReadyIpc;

            if (attempt < maxAttempts - 1)
                await Task.Delay(delayMs, ct).ConfigureAwait(false);
        }

        return ScriptInteractiveReadinessResult.TimedOut;
    }

    internal static CommandResponse CreateInteractiveReadinessFailureResponse(string project, string command)
    {
        var cliCommand = GetCliCommandName(command);
        var primaryAction = $"Run `unityctl status --project \"{project}\" --wait` and retry `{cliCommand}` after the Editor reports Ready.";
        var followUpAction = command == WellKnownCommands.ScriptGetErrors
            ? $"If compilation diagnostics are still missing after Ready, run `unityctl script validate --project \"{project}\" --wait` once to populate the latest compile cache."
            : "Keep the Unity Editor open and let IPC reconnect before retrying this script command. Batch fallback is less reliable for script diagnostics/refactors.";

        return new CommandResponse
        {
            StatusCode = StatusCode.Busy,
            Success = false,
            Message = $"Unity Editor is still compiling or reloading. `{cliCommand}` works best with a running Editor and IPC ready.",
            Data = new JsonObject
            {
                ["command"] = cliCommand,
                ["requiresIpcReady"] = true,
                ["recommendedAction"] = primaryAction,
                ["followUpAction"] = followUpAction
            }
        };
    }

    private static async Task<bool> ProbeIpcAsync(string project, CancellationToken ct)
    {
        await using var ipc = new IpcTransport(project);
        return await ipc.ProbeAsync(ct).ConfigureAwait(false);
    }

    private static string GetCliCommandName(string command)
    {
        return command switch
        {
            WellKnownCommands.ScriptGetErrors => "script get-errors",
            WellKnownCommands.ScriptFindRefs => "script find-refs",
            WellKnownCommands.ScriptRenameSymbol => "script rename-symbol",
            _ => command
        };
    }
}

internal enum ScriptInteractiveReadinessResult
{
    Ready,
    ContinueWithoutReadyIpc,
    TimedOut
}
