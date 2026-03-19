using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Core.Discovery;
using Unityctl.Core.Platform;
using Unityctl.Core.Transport;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class AssetCommand
{
    public static void Find(string project, string filter, string? folder = null, int? limit = null, bool json = false)
    {
        var request = CreateFindRequest(filter, folder, limit);
        CommandRunner.Execute(project, request, json);
    }

    public static void GetInfo(string project, string path, bool json = false)
    {
        var request = CreateGetInfoRequest(path);
        CommandRunner.Execute(project, request, json);
    }

    public static void GetDependencies(string project, string path, string recursive = "true", bool json = false)
    {
        var request = CreateGetDependenciesRequest(path, ParseRecursive(recursive));
        CommandRunner.Execute(project, request, json);
    }

    public static void ReferenceGraph(string project, string path, bool json = false)
    {
        var request = CreateReferenceGraphRequest(path);
        CommandRunner.Execute(project, request, json);
    }

    public static void Refresh(string project, bool noWait = false, bool json = false)
    {
        var exitCode = ExecuteRefreshAsync(project, noWait, json).GetAwaiter().GetResult();
        Environment.Exit(exitCode);
    }

    internal static async Task<int> ExecuteRefreshAsync(string project, bool noWait, bool json)
    {
        var request = CreateRefreshRequest();
        var platform = PlatformFactory.Create();
        var discovery = new UnityEditorDiscovery(platform);
        var executor = new CommandExecutor(platform, discovery);

        CommandResponse response;
        if (noWait)
        {
            response = await executor.ExecuteAsync(project, request);
        }
        else
        {
            response = await AsyncCommandRunner.ExecuteAsync(
                project,
                request,
                async (proj, req, ct) => await executor.ExecuteAsync(proj, req, ct: ct),
                pollCommand: WellKnownCommands.AssetRefreshResult,
                timeoutSeconds: 60,
                timeoutStatusCode: StatusCode.UnknownError,
                timeoutMessage: "Asset refresh timed out after 60s");
        }

        CommandRunner.PrintResponse(response, json);
        return CommandRunner.GetExitCode(response);
    }

    internal static CommandRequest CreateRefreshRequest()
    {
        return new CommandRequest
        {
            Command = WellKnownCommands.AssetRefresh,
            Parameters = new JsonObject()
        };
    }

    internal static CommandRequest CreateFindRequest(string filter, string? folder, int? limit)
    {
        if (string.IsNullOrWhiteSpace(filter))
            throw new ArgumentException("filter must not be empty", nameof(filter));

        var parameters = new JsonObject
        {
            ["filter"] = filter
        };

        if (!string.IsNullOrWhiteSpace(folder))
            parameters["folder"] = folder;

        if (limit.HasValue)
            parameters["limit"] = limit.Value;

        return new CommandRequest
        {
            Command = WellKnownCommands.AssetFind,
            Parameters = parameters
        };
    }

    internal static CommandRequest CreateGetInfoRequest(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path must not be empty", nameof(path));

        return new CommandRequest
        {
            Command = WellKnownCommands.AssetGetInfo,
            Parameters = new JsonObject
            {
                ["path"] = path
            }
        };
    }

    internal static CommandRequest CreateGetDependenciesRequest(string path, bool recursive)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path must not be empty", nameof(path));

        return new CommandRequest
        {
            Command = WellKnownCommands.AssetGetDependencies,
            Parameters = new JsonObject
            {
                ["path"] = path,
                ["recursive"] = recursive
            }
        };
    }

    internal static CommandRequest CreateReferenceGraphRequest(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path must not be empty", nameof(path));

        return new CommandRequest
        {
            Command = WellKnownCommands.AssetReferenceGraph,
            Parameters = new JsonObject
            {
                ["path"] = path
            }
        };
    }

    internal static bool ParseRecursive(string recursive)
    {
        if (string.IsNullOrWhiteSpace(recursive))
            throw new ArgumentException("recursive must not be empty", nameof(recursive));

        if (bool.TryParse(recursive, out var parsed))
            return parsed;

        switch (recursive.Trim().ToLowerInvariant())
        {
            case "1":
            case "on":
            case "yes":
                return true;
            case "0":
            case "off":
            case "no":
                return false;
        }

        throw new ArgumentException("recursive must be 'true' or 'false'", nameof(recursive));
    }

    public static void Create(string project, string path, string type, bool json = false)
    {
        var request = CreateCreateRequest(path, type);
        CommandRunner.Execute(project, request, json);
    }

    public static void CreateFolder(string project, string parent, string name, bool json = false)
    {
        var request = CreateCreateFolderRequest(parent, name);
        CommandRunner.Execute(project, request, json);
    }

    public static void Copy(string project, string source, string destination, bool json = false)
    {
        var request = CreateCopyRequest(source, destination);
        CommandRunner.Execute(project, request, json);
    }

    public static void Move(string project, string source, string destination, bool json = false)
    {
        var request = CreateMoveRequest(source, destination);
        CommandRunner.Execute(project, request, json);
    }

    public static void Delete(string project, string path, bool json = false)
    {
        var request = CreateDeleteRequest(path);
        CommandRunner.Execute(project, request, json);
    }

    public static void GetLabels(string project, string path, bool json = false)
    {
        var request = CreateGetLabelsRequest(path);
        CommandRunner.Execute(project, request, json);
    }

    public static void SetLabels(string project, string path, string labels, bool json = false)
    {
        var request = CreateSetLabelsRequest(path, labels);
        CommandRunner.Execute(project, request, json);
    }

    public static void Import(string project, string path, string? options = null, bool json = false)
    {
        var request = CreateImportRequest(path, options);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateCreateRequest(string path, string type)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path must not be empty", nameof(path));
        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("type must not be empty", nameof(type));

        return new CommandRequest
        {
            Command = WellKnownCommands.AssetCreate,
            Parameters = new JsonObject
            {
                ["path"] = path,
                ["type"] = type
            }
        };
    }

    internal static CommandRequest CreateCreateFolderRequest(string parent, string name)
    {
        if (string.IsNullOrWhiteSpace(parent))
            throw new ArgumentException("parent must not be empty", nameof(parent));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("name must not be empty", nameof(name));

        return new CommandRequest
        {
            Command = WellKnownCommands.AssetCreateFolder,
            Parameters = new JsonObject
            {
                ["parent"] = parent,
                ["name"] = name
            }
        };
    }

    internal static CommandRequest CreateCopyRequest(string source, string destination)
    {
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("source must not be empty", nameof(source));
        if (string.IsNullOrWhiteSpace(destination))
            throw new ArgumentException("destination must not be empty", nameof(destination));

        return new CommandRequest
        {
            Command = WellKnownCommands.AssetCopy,
            Parameters = new JsonObject
            {
                ["source"] = source,
                ["destination"] = destination
            }
        };
    }

    internal static CommandRequest CreateMoveRequest(string source, string destination)
    {
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("source must not be empty", nameof(source));
        if (string.IsNullOrWhiteSpace(destination))
            throw new ArgumentException("destination must not be empty", nameof(destination));

        return new CommandRequest
        {
            Command = WellKnownCommands.AssetMove,
            Parameters = new JsonObject
            {
                ["source"] = source,
                ["destination"] = destination
            }
        };
    }

    internal static CommandRequest CreateDeleteRequest(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path must not be empty", nameof(path));

        return new CommandRequest
        {
            Command = WellKnownCommands.AssetDelete,
            Parameters = new JsonObject { ["path"] = path }
        };
    }

    internal static CommandRequest CreateImportRequest(string path, string? options)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path must not be empty", nameof(path));

        var parameters = new JsonObject { ["path"] = path };
        if (!string.IsNullOrEmpty(options)) parameters["options"] = options;

        return new CommandRequest
        {
            Command = WellKnownCommands.AssetImport,
            Parameters = parameters
        };
    }

    internal static CommandRequest CreateGetLabelsRequest(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path must not be empty", nameof(path));

        return new CommandRequest
        {
            Command = WellKnownCommands.AssetGetLabels,
            Parameters = new JsonObject { ["path"] = path }
        };
    }

    internal static CommandRequest CreateSetLabelsRequest(string path, string labels)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path must not be empty", nameof(path));
        if (labels is null)
            throw new ArgumentException("labels must not be null", nameof(labels));

        return new CommandRequest
        {
            Command = WellKnownCommands.AssetSetLabels,
            Parameters = new JsonObject
            {
                ["path"] = path,
                ["labels"] = labels
            }
        };
    }
}
