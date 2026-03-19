using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class GameObjectCommand
{
    public static void Find(
        string project,
        string? name = null,
        string? tag = null,
        string? layer = null,
        string? component = null,
        string? scene = null,
        bool includeInactive = false,
        int? limit = null,
        bool json = false)
    {
        var request = CreateFindRequest(name, tag, layer, component, scene, includeInactive, limit);
        CommandRunner.Execute(project, request, json);
    }

    public static void Get(string project, string id, bool json = false)
    {
        var request = CreateGetRequest(id);
        CommandRunner.Execute(project, request, json);
    }

    public static void Create(string project, string name, string? parent = null, string? scene = null, bool json = false)
    {
        var request = CreateCreateRequest(name, parent, scene);
        CommandRunner.Execute(project, request, json);
    }

    public static void Delete(string project, string id, bool json = false)
    {
        var request = CreateDeleteRequest(id);
        CommandRunner.Execute(project, request, json);
    }

    public static void SetActive(string project, string id, string active, bool json = false)
    {
        var request = CreateSetActiveRequest(id, ParseActive(active));
        CommandRunner.Execute(project, request, json);
    }

    public static void Activate(string project, string id, bool json = false)
    {
        var request = CreateSetActiveRequest(id, true);
        CommandRunner.Execute(project, request, json);
    }

    public static void Deactivate(string project, string id, bool json = false)
    {
        var request = CreateSetActiveRequest(id, false);
        CommandRunner.Execute(project, request, json);
    }

    public static void Move(string project, string id, string parent, bool json = false)
    {
        var request = CreateMoveRequest(id, parent);
        CommandRunner.Execute(project, request, json);
    }

    public static void Rename(string project, string id, string name, bool json = false)
    {
        var request = CreateRenameRequest(id, name);
        CommandRunner.Execute(project, request, json);
    }

    public static void SetTag(string project, string id, string tag, bool json = false)
    {
        var request = CreateSetTagRequest(id, tag);
        CommandRunner.Execute(project, request, json);
    }

    public static void SetLayer(string project, string id, string layer, bool json = false)
    {
        var request = CreateSetLayerRequest(id, layer);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateCreateRequest(string name, string? parent, string? scene)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("name must not be empty", nameof(name));

        var parameters = new JsonObject { ["name"] = name };
        if (!string.IsNullOrEmpty(parent)) parameters["parent"] = parent;
        if (!string.IsNullOrEmpty(scene)) parameters["scene"] = scene;

        return new CommandRequest
        {
            Command = WellKnownCommands.GameObjectCreate,
            Parameters = parameters
        };
    }

    internal static CommandRequest CreateFindRequest(
        string? name,
        string? tag,
        string? layer,
        string? component,
        string? scene,
        bool includeInactive,
        int? limit)
    {
        var parameters = new JsonObject();
        if (!string.IsNullOrWhiteSpace(name)) parameters["name"] = name;
        if (!string.IsNullOrWhiteSpace(tag)) parameters["tag"] = tag;
        if (!string.IsNullOrWhiteSpace(layer)) parameters["layer"] = layer;
        if (!string.IsNullOrWhiteSpace(component)) parameters["component"] = component;
        if (!string.IsNullOrWhiteSpace(scene)) parameters["scene"] = scene;
        if (includeInactive) parameters["includeInactive"] = true;
        if (limit.HasValue) parameters["limit"] = limit.Value;

        return new CommandRequest
        {
            Command = WellKnownCommands.GameObjectFind,
            Parameters = parameters
        };
    }

    internal static CommandRequest CreateGetRequest(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("id must not be empty", nameof(id));

        return new CommandRequest
        {
            Command = WellKnownCommands.GameObjectGet,
            Parameters = new JsonObject { ["id"] = id }
        };
    }

    internal static CommandRequest CreateDeleteRequest(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("id must not be empty", nameof(id));

        return new CommandRequest
        {
            Command = WellKnownCommands.GameObjectDelete,
            Parameters = new JsonObject { ["id"] = id }
        };
    }

    internal static CommandRequest CreateSetActiveRequest(string id, bool active)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("id must not be empty", nameof(id));

        return new CommandRequest
        {
            Command = WellKnownCommands.GameObjectSetActive,
            Parameters = new JsonObject
            {
                ["id"] = id,
                ["active"] = active
            }
        };
    }

    internal static bool ParseActive(string active)
    {
        if (string.IsNullOrWhiteSpace(active))
            throw new ArgumentException("active must not be empty", nameof(active));

        if (bool.TryParse(active, out var parsed))
            return parsed;

        switch (active.Trim().ToLowerInvariant())
        {
            case "1":
            case "on":
            case "enable":
            case "enabled":
            case "active":
                return true;
            case "0":
            case "off":
            case "disable":
            case "disabled":
            case "inactive":
                return false;
        }

        throw new ArgumentException("active must be 'true' or 'false'", nameof(active));
    }

    internal static CommandRequest CreateMoveRequest(string id, string parent)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("id must not be empty", nameof(id));
        if (string.IsNullOrWhiteSpace(parent))
            throw new ArgumentException("parent must not be empty", nameof(parent));

        return new CommandRequest
        {
            Command = WellKnownCommands.GameObjectMove,
            Parameters = new JsonObject
            {
                ["id"] = id,
                ["parent"] = parent
            }
        };
    }

    internal static CommandRequest CreateRenameRequest(string id, string name)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("id must not be empty", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("name must not be empty", nameof(name));

        return new CommandRequest
        {
            Command = WellKnownCommands.GameObjectRename,
            Parameters = new JsonObject
            {
                ["id"] = id,
                ["name"] = name
            }
        };
    }

    internal static CommandRequest CreateSetTagRequest(string id, string tag)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("id must not be empty", nameof(id));
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("tag must not be empty", nameof(tag));

        return new CommandRequest
        {
            Command = WellKnownCommands.GameObjectSetTag,
            Parameters = new JsonObject
            {
                ["id"] = id,
                ["tag"] = tag
            }
        };
    }

    internal static CommandRequest CreateSetLayerRequest(string id, string layer)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("id must not be empty", nameof(id));
        if (string.IsNullOrWhiteSpace(layer))
            throw new ArgumentException("layer must not be empty", nameof(layer));

        return new CommandRequest
        {
            Command = WellKnownCommands.GameObjectSetLayer,
            Parameters = new JsonObject
            {
                ["id"] = id,
                ["layer"] = layer
            }
        };
    }
}
