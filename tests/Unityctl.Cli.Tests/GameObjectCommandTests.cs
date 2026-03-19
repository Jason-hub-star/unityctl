using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class GameObjectCommandTests
{
    // === Find ===

    [CliTestFact]
    public void Find_SetsCommandName()
    {
        var request = GameObjectCommand.CreateFindRequest("Camera", null, null, null, null, false, null);
        Assert.Equal(WellKnownCommands.GameObjectFind, request.Command);
    }

    [CliTestFact]
    public void Find_SetsProvidedParameters()
    {
        var request = GameObjectCommand.CreateFindRequest(
            "Camera",
            "MainCamera",
            "0",
            "UnityEngine.Camera",
            "Assets/Scenes/Main.unity",
            includeInactive: true,
            limit: 5);

        Assert.Equal("Camera", request.Parameters!["name"]?.GetValue<string>());
        Assert.Equal("MainCamera", request.Parameters["tag"]?.GetValue<string>());
        Assert.Equal("0", request.Parameters["layer"]?.GetValue<string>());
        Assert.Equal("UnityEngine.Camera", request.Parameters["component"]?.GetValue<string>());
        Assert.Equal("Assets/Scenes/Main.unity", request.Parameters["scene"]?.GetValue<string>());
        Assert.True(request.Parameters["includeInactive"]?.GetValue<bool>());
        Assert.Equal(5, request.Parameters["limit"]?.GetValue<int>());
    }

    [CliTestFact]
    public void Find_OmitsUnsetParameters()
    {
        var request = GameObjectCommand.CreateFindRequest(null, null, null, null, null, false, null);

        Assert.False(request.Parameters!.ContainsKey("name"));
        Assert.False(request.Parameters.ContainsKey("tag"));
        Assert.False(request.Parameters.ContainsKey("layer"));
        Assert.False(request.Parameters.ContainsKey("component"));
        Assert.False(request.Parameters.ContainsKey("scene"));
        Assert.False(request.Parameters.ContainsKey("includeInactive"));
        Assert.False(request.Parameters.ContainsKey("limit"));
    }

    // === Get ===

    [CliTestFact]
    public void Get_SetsCommandName()
    {
        var request = GameObjectCommand.CreateGetRequest("gid-123");
        Assert.Equal(WellKnownCommands.GameObjectGet, request.Command);
    }

    [CliTestFact]
    public void Get_SetsIdParameter()
    {
        var request = GameObjectCommand.CreateGetRequest("gid-123");
        Assert.Equal("gid-123", request.Parameters!["id"]?.GetValue<string>());
    }

    [CliTestFact]
    public void Get_EmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(() => GameObjectCommand.CreateGetRequest(""));
    }

    // === Create ===

    [CliTestFact]
    public void Create_SetsCommandName()
    {
        var request = GameObjectCommand.CreateCreateRequest("Cube", null, null);
        Assert.Equal(WellKnownCommands.GameObjectCreate, request.Command);
    }

    [CliTestFact]
    public void Create_SetsNameParameter()
    {
        var request = GameObjectCommand.CreateCreateRequest("TestObj", null, null);
        Assert.Equal("TestObj", request.Parameters!["name"]?.GetValue<string>());
    }

    [CliTestFact]
    public void Create_SetsParentWhenProvided()
    {
        var request = GameObjectCommand.CreateCreateRequest("Child", "GlobalObjectId_V1-2-abc", null);
        Assert.Equal("GlobalObjectId_V1-2-abc", request.Parameters!["parent"]?.GetValue<string>());
    }

    [CliTestFact]
    public void Create_SetsSceneWhenProvided()
    {
        var request = GameObjectCommand.CreateCreateRequest("Obj", null, "Assets/Scenes/Main.unity");
        Assert.Equal("Assets/Scenes/Main.unity", request.Parameters!["scene"]?.GetValue<string>());
    }

    [CliTestFact]
    public void Create_OmitsParentAndSceneWhenNull()
    {
        var request = GameObjectCommand.CreateCreateRequest("Obj", null, null);
        Assert.False(request.Parameters!.ContainsKey("parent"));
        Assert.False(request.Parameters!.ContainsKey("scene"));
    }

    [CliTestFact]
    public void Create_EmptyName_Throws()
    {
        Assert.Throws<ArgumentException>(() => GameObjectCommand.CreateCreateRequest("", null, null));
    }

    // === Delete ===

    [CliTestFact]
    public void Delete_SetsCommandName()
    {
        var request = GameObjectCommand.CreateDeleteRequest("GlobalObjectId_V1-2-xyz");
        Assert.Equal(WellKnownCommands.GameObjectDelete, request.Command);
    }

    [CliTestFact]
    public void Delete_SetsIdParameter()
    {
        var request = GameObjectCommand.CreateDeleteRequest("gid-123");
        Assert.Equal("gid-123", request.Parameters!["id"]?.GetValue<string>());
    }

    [CliTestFact]
    public void Delete_EmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(() => GameObjectCommand.CreateDeleteRequest(""));
    }

    // === SetActive ===

    [CliTestFact]
    public void SetActive_SetsCommandName()
    {
        var request = GameObjectCommand.CreateSetActiveRequest("gid", true);
        Assert.Equal(WellKnownCommands.GameObjectSetActive, request.Command);
    }

    [CliTestFact]
    public void SetActive_SetsActiveTrue()
    {
        var request = GameObjectCommand.CreateSetActiveRequest("gid", true);
        Assert.True(request.Parameters!["active"]?.GetValue<bool>());
    }

    [CliTestFact]
    public void SetActive_SetsActiveFalse()
    {
        var request = GameObjectCommand.CreateSetActiveRequest("gid", false);
        Assert.False(request.Parameters!["active"]?.GetValue<bool>());
    }

    [CliTestFact]
    public void SetActive_EmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(() => GameObjectCommand.CreateSetActiveRequest("", true));
    }

    [CliTestFact]
    public void SetActive_ParseActive_ParsesTrue()
    {
        Assert.True(GameObjectCommand.ParseActive("true"));
    }

    [CliTestFact]
    public void SetActive_ParseActive_ParsesFalse()
    {
        Assert.False(GameObjectCommand.ParseActive("false"));
    }

    [CliTestFact]
    public void SetActive_ParseActive_Invalid_Throws()
    {
        Assert.Throws<ArgumentException>(() => GameObjectCommand.ParseActive("nope"));
    }

    [CliTestFact]
    public void SetActive_ParseActive_ParsesOn()
    {
        Assert.True(GameObjectCommand.ParseActive("on"));
    }

    [CliTestFact]
    public void SetActive_ParseActive_ParsesOff()
    {
        Assert.False(GameObjectCommand.ParseActive("off"));
    }

    // === Move ===

    [CliTestFact]
    public void Move_SetsCommandName()
    {
        var request = GameObjectCommand.CreateMoveRequest("child-gid", "parent-gid");
        Assert.Equal(WellKnownCommands.GameObjectMove, request.Command);
    }

    [CliTestFact]
    public void Move_SetsIdAndParent()
    {
        var request = GameObjectCommand.CreateMoveRequest("child-gid", "parent-gid");
        Assert.Equal("child-gid", request.Parameters!["id"]?.GetValue<string>());
        Assert.Equal("parent-gid", request.Parameters!["parent"]?.GetValue<string>());
    }

    [CliTestFact]
    public void Move_EmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(() => GameObjectCommand.CreateMoveRequest("", "p"));
    }

    [CliTestFact]
    public void Move_EmptyParent_Throws()
    {
        Assert.Throws<ArgumentException>(() => GameObjectCommand.CreateMoveRequest("c", ""));
    }

    // === Rename ===

    [CliTestFact]
    public void Rename_SetsCommandName()
    {
        var request = GameObjectCommand.CreateRenameRequest("gid", "NewName");
        Assert.Equal(WellKnownCommands.GameObjectRename, request.Command);
    }

    [CliTestFact]
    public void Rename_SetsIdAndName()
    {
        var request = GameObjectCommand.CreateRenameRequest("gid", "NewName");
        Assert.Equal("gid", request.Parameters!["id"]?.GetValue<string>());
        Assert.Equal("NewName", request.Parameters!["name"]?.GetValue<string>());
    }

    [CliTestFact]
    public void Rename_EmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(() => GameObjectCommand.CreateRenameRequest("", "name"));
    }

    [CliTestFact]
    public void Rename_EmptyName_Throws()
    {
        Assert.Throws<ArgumentException>(() => GameObjectCommand.CreateRenameRequest("gid", ""));
    }

    // === SetTag ===

    [CliTestFact]
    public void SetTag_SetsCommandName()
    {
        var request = GameObjectCommand.CreateSetTagRequest("gid", "Player");
        Assert.Equal(WellKnownCommands.GameObjectSetTag, request.Command);
    }

    [CliTestFact]
    public void SetTag_SetsIdAndTag()
    {
        var request = GameObjectCommand.CreateSetTagRequest("gid-123", "Enemy");
        Assert.Equal("gid-123", request.Parameters!["id"]?.GetValue<string>());
        Assert.Equal("Enemy", request.Parameters!["tag"]?.GetValue<string>());
    }

    [CliTestFact]
    public void SetTag_EmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(() => GameObjectCommand.CreateSetTagRequest("", "Tag"));
    }

    [CliTestFact]
    public void SetTag_EmptyTag_Throws()
    {
        Assert.Throws<ArgumentException>(() => GameObjectCommand.CreateSetTagRequest("gid", ""));
    }

    // === SetLayer ===

    [CliTestFact]
    public void SetLayer_SetsCommandName()
    {
        var request = GameObjectCommand.CreateSetLayerRequest("gid", "Water");
        Assert.Equal(WellKnownCommands.GameObjectSetLayer, request.Command);
    }

    [CliTestFact]
    public void SetLayer_SetsIdAndLayer()
    {
        var request = GameObjectCommand.CreateSetLayerRequest("gid-123", "8");
        Assert.Equal("gid-123", request.Parameters!["id"]?.GetValue<string>());
        Assert.Equal("8", request.Parameters!["layer"]?.GetValue<string>());
    }

    [CliTestFact]
    public void SetLayer_EmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(() => GameObjectCommand.CreateSetLayerRequest("", "Water"));
    }

    [CliTestFact]
    public void SetLayer_EmptyLayer_Throws()
    {
        Assert.Throws<ArgumentException>(() => GameObjectCommand.CreateSetLayerRequest("gid", ""));
    }

    // === SceneSave ===

    [CliTestFact]
    public void SceneSave_SetsCommandName()
    {
        var request = SceneCommand.CreateSaveRequest(null, false);
        Assert.Equal(WellKnownCommands.SceneSave, request.Command);
    }

    [CliTestFact]
    public void SceneSave_SetsSceneWhenProvided()
    {
        var request = SceneCommand.CreateSaveRequest("Assets/Scenes/Main.unity", false);
        Assert.Equal("Assets/Scenes/Main.unity", request.Parameters!["scene"]?.GetValue<string>());
    }

    [CliTestFact]
    public void SceneSave_SetsAllWhenTrue()
    {
        var request = SceneCommand.CreateSaveRequest(null, true);
        Assert.True(request.Parameters!["all"]?.GetValue<bool>());
    }

    [CliTestFact]
    public void SceneSave_OmitsSceneAndAllWhenDefaults()
    {
        var request = SceneCommand.CreateSaveRequest(null, false);
        Assert.False(request.Parameters!.ContainsKey("scene"));
        Assert.False(request.Parameters!.ContainsKey("all"));
    }

    // === HasRequestId ===

    [CliTestFact]
    public void AllRequests_HaveRequestId()
    {
        Assert.False(string.IsNullOrEmpty(GameObjectCommand.CreateFindRequest(null, null, null, null, null, false, null).RequestId));
        Assert.False(string.IsNullOrEmpty(GameObjectCommand.CreateGetRequest("x").RequestId));
        Assert.False(string.IsNullOrEmpty(GameObjectCommand.CreateCreateRequest("x", null, null).RequestId));
        Assert.False(string.IsNullOrEmpty(GameObjectCommand.CreateDeleteRequest("x").RequestId));
        Assert.False(string.IsNullOrEmpty(GameObjectCommand.CreateSetActiveRequest("x", true).RequestId));
        Assert.False(string.IsNullOrEmpty(GameObjectCommand.CreateMoveRequest("x", "y").RequestId));
        Assert.False(string.IsNullOrEmpty(GameObjectCommand.CreateRenameRequest("x", "y").RequestId));
        Assert.False(string.IsNullOrEmpty(GameObjectCommand.CreateSetTagRequest("x", "y").RequestId));
        Assert.False(string.IsNullOrEmpty(GameObjectCommand.CreateSetLayerRequest("x", "y").RequestId));
        Assert.False(string.IsNullOrEmpty(SceneCommand.CreateSaveRequest(null, false).RequestId));
    }
}
