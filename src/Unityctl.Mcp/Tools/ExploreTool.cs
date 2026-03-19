using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol.Server;
using Unityctl.Core.Transport;
using Unityctl.Shared.Protocol;
using Unityctl.Shared.Serialization;

namespace Unityctl.Mcp.Tools;

[McpServerToolType]
internal sealed class ExploreTool(CommandExecutor executor)
{
    [McpServerTool(Name = "unityctl_asset_find")]
    [Description("Find assets using Unity AssetDatabase.FindAssets filter syntax")]
    public Task<string> AssetFindAsync(
        [Description("Path to the Unity project directory")] string project,
        [Description("AssetDatabase.FindAssets filter (for example: t:Scene, l:tag)")] string filter,
        [Description("Optional root folder to search under")] string? folder = null,
        [Description("Maximum number of results to return")] int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var parameters = new JsonObject
        {
            ["filter"] = filter
        };
        if (!string.IsNullOrWhiteSpace(folder)) parameters["folder"] = folder;
        if (limit.HasValue) parameters["limit"] = limit.Value;

        return ExecuteAsync(project, WellKnownCommands.AssetFind, parameters, cancellationToken);
    }

    [McpServerTool(Name = "unityctl_asset_get_info")]
    [Description("Get summary information for a single asset path")]
    public Task<string> AssetGetInfoAsync(
        [Description("Path to the Unity project directory")] string project,
        [Description("Asset path (for example: Assets/Scenes/Main.unity)")] string path,
        CancellationToken cancellationToken = default)
    {
        var parameters = new JsonObject
        {
            ["path"] = path
        };

        return ExecuteAsync(project, WellKnownCommands.AssetGetInfo, parameters, cancellationToken);
    }

    [McpServerTool(Name = "unityctl_asset_get_dependencies")]
    [Description("Get asset dependency paths using Unity AssetDatabase.GetDependencies")]
    public Task<string> AssetGetDependenciesAsync(
        [Description("Path to the Unity project directory")] string project,
        [Description("Asset path (for example: Assets/Scenes/Main.unity)")] string path,
        [Description("Include indirect dependencies (default: true)")] bool recursive = true,
        CancellationToken cancellationToken = default)
    {
        var parameters = new JsonObject
        {
            ["path"] = path,
            ["recursive"] = recursive
        };

        return ExecuteAsync(project, WellKnownCommands.AssetGetDependencies, parameters, cancellationToken);
    }

    [McpServerTool(Name = "unityctl_asset_reference_graph")]
    [Description("Find reverse references to an asset by scanning candidate assets and their dependencies")]
    public Task<string> AssetReferenceGraphAsync(
        [Description("Path to the Unity project directory")] string project,
        [Description("Target asset path (for example: Assets/Materials/My.mat)")] string path,
        CancellationToken cancellationToken = default)
    {
        var parameters = new JsonObject
        {
            ["path"] = path
        };

        return ExecuteAsync(project, WellKnownCommands.AssetReferenceGraph, parameters, cancellationToken);
    }

    [McpServerTool(Name = "unityctl_asset_get_labels")]
    [Description("Get all labels attached to an asset")]
    public Task<string> AssetGetLabelsAsync(
        [Description("Path to the Unity project directory")] string project,
        [Description("Asset path (for example: Assets/Textures/Road.png)")] string path,
        CancellationToken cancellationToken = default)
    {
        var parameters = new JsonObject { ["path"] = path };
        return ExecuteAsync(project, WellKnownCommands.AssetGetLabels, parameters, cancellationToken);
    }

    [McpServerTool(Name = "unityctl_build_settings_get_scenes")]
    [Description("Get the current Build Settings scene list from EditorBuildSettings.scenes")]
    public Task<string> BuildSettingsGetScenesAsync(
        [Description("Path to the Unity project directory")] string project,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(project, WellKnownCommands.BuildSettingsGetScenes, new JsonObject(), cancellationToken);
    }

    [McpServerTool(Name = "unityctl_gameobject_find")]
    [Description("Find GameObjects in loaded scenes using narrow query filters")]
    public Task<string> GameObjectFindAsync(
        [Description("Path to the Unity project directory")] string project,
        [Description("Case-insensitive partial match on GameObject name")] string? name = null,
        [Description("Exact match on GameObject tag")] string? tag = null,
        [Description("Exact match on layer name or numeric layer index")] string? layer = null,
        [Description("Exact match on component type name or full type name")] string? component = null,
        [Description("Scene asset path filter")] string? scene = null,
        [Description("Include inactive GameObjects in the search")] bool includeInactive = false,
        [Description("Maximum number of results to return")] int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var parameters = new JsonObject();
        if (!string.IsNullOrWhiteSpace(name)) parameters["name"] = name;
        if (!string.IsNullOrWhiteSpace(tag)) parameters["tag"] = tag;
        if (!string.IsNullOrWhiteSpace(layer)) parameters["layer"] = layer;
        if (!string.IsNullOrWhiteSpace(component)) parameters["component"] = component;
        if (!string.IsNullOrWhiteSpace(scene)) parameters["scene"] = scene;
        if (includeInactive) parameters["includeInactive"] = true;
        if (limit.HasValue) parameters["limit"] = limit.Value;

        return ExecuteAsync(project, WellKnownCommands.GameObjectFind, parameters, cancellationToken);
    }

    [McpServerTool(Name = "unityctl_gameobject_get")]
    [Description("Get summary details for a single GameObject by GlobalObjectId")]
    public Task<string> GameObjectGetAsync(
        [Description("Path to the Unity project directory")] string project,
        [Description("GlobalObjectId of the GameObject")] string id,
        CancellationToken cancellationToken = default)
    {
        var parameters = new JsonObject
        {
            ["id"] = id
        };

        return ExecuteAsync(project, WellKnownCommands.GameObjectGet, parameters, cancellationToken);
    }

    [McpServerTool(Name = "unityctl_component_get")]
    [Description("Get summary or serialized property details for a single component")]
    public Task<string> ComponentGetAsync(
        [Description("Path to the Unity project directory")] string project,
        [Description("GlobalObjectId of the component")] string componentId,
        [Description("SerializedProperty path to read (optional, returns all if omitted)")] string? property = null,
        CancellationToken cancellationToken = default)
    {
        var parameters = new JsonObject
        {
            ["componentId"] = componentId
        };
        if (!string.IsNullOrWhiteSpace(property)) parameters["property"] = property;

        return ExecuteAsync(project, WellKnownCommands.ComponentGet, parameters, cancellationToken);
    }

    [McpServerTool(Name = "unityctl_tag_list")]
    [Description("List all tags defined in the Unity project")]
    public Task<string> TagListAsync(
        [Description("Path to the Unity project directory")] string project,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(project, WellKnownCommands.TagList, new JsonObject(), cancellationToken);
    }

    [McpServerTool(Name = "unityctl_layer_list")]
    [Description("List all 32 layer slots with names and built-in flags")]
    public Task<string> LayerListAsync(
        [Description("Path to the Unity project directory")] string project,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(project, WellKnownCommands.LayerList, new JsonObject(), cancellationToken);
    }

    [McpServerTool(Name = "unityctl_console_get_count")]
    [Description("Get the count of log messages, warnings, and errors in the Unity console")]
    public Task<string> ConsoleGetCountAsync(
        [Description("Path to the Unity project directory")] string project,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(project, WellKnownCommands.ConsoleGetCount, new JsonObject(), cancellationToken);
    }

    [McpServerTool(Name = "unityctl_define_symbols_get")]
    [Description("Get scripting define symbols for the active build target")]
    public Task<string> DefineSymbolsGetAsync(
        [Description("Path to the Unity project directory")] string project,
        [Description("Named build target (optional, defaults to active)")] string? target = null,
        CancellationToken cancellationToken = default)
    {
        var parameters = new JsonObject();
        if (!string.IsNullOrWhiteSpace(target)) parameters["target"] = target;

        return ExecuteAsync(project, WellKnownCommands.DefineSymbolsGet, parameters, cancellationToken);
    }

    [McpServerTool(Name = "unityctl_lighting_get_settings")]
    [Description("Get the current scene lighting settings")]
    public Task<string> LightingGetSettingsAsync(
        [Description("Path to the Unity project directory")] string project,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(project, WellKnownCommands.LightingGetSettings, new JsonObject(), cancellationToken);
    }

    [McpServerTool(Name = "unityctl_navmesh_get_settings")]
    [Description("Get NavMesh build settings for all agent types")]
    public Task<string> NavMeshGetSettingsAsync(
        [Description("Path to the Unity project directory")] string project,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(project, WellKnownCommands.NavMeshGetSettings, new JsonObject(), cancellationToken);
    }

    [McpServerTool(Name = "unityctl_script_list")]
    [Description("List MonoScript assets in the Unity project")]
    public Task<string> ScriptListAsync(
        [Description("Path to the Unity project directory")] string project,
        [Description("Root folder to search under")] string? folder = null,
        [Description("Case-insensitive name filter")] string? filter = null,
        [Description("Maximum number of results")] int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var parameters = new JsonObject();
        if (!string.IsNullOrWhiteSpace(folder)) parameters["folder"] = folder;
        if (!string.IsNullOrWhiteSpace(filter)) parameters["filter"] = filter;
        if (limit.HasValue) parameters["limit"] = limit.Value;

        return ExecuteAsync(project, WellKnownCommands.ScriptList, parameters, cancellationToken);
    }

    [McpServerTool(Name = "unityctl_physics_get_settings")]
    [Description("Get all physics settings from DynamicsManager (gravity, solver iterations, etc.)")]
    public Task<string> PhysicsGetSettingsAsync(
        [Description("Path to the Unity project directory")] string project,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(project, WellKnownCommands.PhysicsGetSettings, new JsonObject(), cancellationToken);
    }

    [McpServerTool(Name = "unityctl_physics_get_collision_matrix")]
    [Description("Get the 32x32 layer collision matrix showing which layers collide")]
    public Task<string> PhysicsGetCollisionMatrixAsync(
        [Description("Path to the Unity project directory")] string project,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(project, WellKnownCommands.PhysicsGetCollisionMatrix, new JsonObject(), cancellationToken);
    }

    private async Task<string> ExecuteAsync(
        string project,
        string command,
        JsonObject parameters,
        CancellationToken cancellationToken)
    {
        var request = new CommandRequest
        {
            Command = command,
            Parameters = parameters
        };

        var response = await executor.ExecuteAsync(project, request, ct: cancellationToken);
        return JsonSerializer.Serialize(response, UnityctlJsonContext.Default.CommandResponse);
    }
}
