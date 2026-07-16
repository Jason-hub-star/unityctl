using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class ScriptCommandTests
{
    [Fact]
    public void CreateListRequest_HasCorrectCommand()
    {
        var request = ScriptCommand.CreateListRequest();
        Assert.Equal(WellKnownCommands.ScriptList, request.Command);
        Assert.NotNull(request.RequestId);
    }

    [Fact]
    public void CreateListRequest_SetsFolderParameter()
    {
        var request = ScriptCommand.CreateListRequest(folder: "Assets/Scripts");
        Assert.Equal("Assets/Scripts", request.Parameters!["folder"]!.ToString());
    }

    [Fact]
    public void CreateListRequest_SetsFilterParameter()
    {
        var request = ScriptCommand.CreateListRequest(filter: "Player");
        Assert.Equal("Player", request.Parameters!["filter"]!.ToString());
    }

    [Fact]
    public void CreateListRequest_SetsLimitParameter()
    {
        var request = ScriptCommand.CreateListRequest(limit: 10);
        Assert.Equal(10, (int)request.Parameters!["limit"]!);
    }

    [Fact]
    public void CreateListRequest_NoOptionalParams_HasEmptyParameters()
    {
        var request = ScriptCommand.CreateListRequest();
        Assert.Empty(request.Parameters!);
    }

    [Fact]
    public void CreateCreateRequest_HasCorrectCommand()
    {
        var request = ScriptCommand.CreateCreateRequest("Assets/Scripts/Test.cs", "Test", null, "MonoBehaviour");
        Assert.Equal(WellKnownCommands.ScriptCreate, request.Command);
    }

    [Fact]
    public void CreateCreateRequest_EmptyPath_Throws()
    {
        Assert.Throws<ArgumentException>(() => ScriptCommand.CreateCreateRequest("", "Test", null, "MonoBehaviour"));
    }

    [Fact]
    public void CreateCreateRequest_NoContent_OmitsContentParam()
    {
        var request = ScriptCommand.CreateCreateRequest("Assets/Scripts/Test.cs", "Test", null, "MonoBehaviour");
        Assert.Null(request.Parameters!["content"]);
    }

    [Fact]
    public void CreateCreateRequest_WithContent_SetsContentParam()
    {
        var request = ScriptCommand.CreateCreateRequest("Assets/Scripts/Test.cs", "Test", null, "MonoBehaviour", "public class Test {}");
        Assert.Equal("public class Test {}", request.Parameters!["content"]!.ToString());
    }

    [Fact]
    public void CreateEditRequest_HasCorrectCommand()
    {
        var request = ScriptCommand.CreateEditRequest("Assets/Scripts/Test.cs", "using UnityEngine;");
        Assert.Equal(WellKnownCommands.ScriptEdit, request.Command);
    }

    [Fact]
    public void CreateDeleteRequest_HasCorrectCommand()
    {
        var request = ScriptCommand.CreateDeleteRequest("Assets/Scripts/Test.cs");
        Assert.Equal(WellKnownCommands.ScriptDelete, request.Command);
    }

    [Fact]
    public void CreateValidateRequest_HasCorrectCommand()
    {
        var request = ScriptCommand.CreateValidateRequest(null);
        Assert.Equal(WellKnownCommands.ScriptValidate, request.Command);
    }

    [Fact]
    public void CreatePatchRequest_HasCorrectCommand()
    {
        var request = ScriptCommand.CreatePatchRequest("Assets/Scripts/Test.cs", 5, 1, "// patched");
        Assert.Equal(WellKnownCommands.ScriptPatch, request.Command);
    }

    [Fact]
    public void CreatePatchRequest_EmptyPath_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ScriptCommand.CreatePatchRequest("", 1, 0, "// test"));
    }

    [Fact]
    public void CreatePatchRequest_SetsPathParameter()
    {
        var request = ScriptCommand.CreatePatchRequest("Assets/Scripts/Test.cs", 5, 2, "// new");
        Assert.Equal("Assets/Scripts/Test.cs", request.Parameters!["path"]!.ToString());
    }

    [Fact]
    public void CreatePatchRequest_SetsStartLine()
    {
        var request = ScriptCommand.CreatePatchRequest("Assets/Scripts/Test.cs", 10, 0, "// header");
        Assert.Equal(10, (int)request.Parameters!["startLine"]!);
    }

    [Fact]
    public void CreatePatchRequest_SetsDeleteCount()
    {
        var request = ScriptCommand.CreatePatchRequest("Assets/Scripts/Test.cs", 3, 5, null);
        Assert.Equal(5, (int)request.Parameters!["deleteCount"]!);
    }

    [Fact]
    public void CreatePatchRequest_InsertOnly_NoContent_OmitsKey()
    {
        var request = ScriptCommand.CreatePatchRequest("Assets/Scripts/Test.cs", 1, 3, null);
        Assert.Null(request.Parameters!["insertContent"]);
    }

    [Fact]
    public void CreatePatchRequest_SetsInsertContent()
    {
        var request = ScriptCommand.CreatePatchRequest("Assets/Scripts/Test.cs", 1, 0, "using System;\nusing UnityEngine;");
        Assert.Equal("using System;\nusing UnityEngine;", request.Parameters!["insertContent"]!.ToString());
    }

    // script-get-errors tests
    [Fact]
    public void CreateGetErrorsRequest_HasCorrectCommand()
    {
        var request = ScriptCommand.CreateGetErrorsRequest();
        Assert.Equal(WellKnownCommands.ScriptGetErrors, request.Command);
    }

    [Fact]
    public void CreateGetErrorsRequest_SetsPathFilter()
    {
        var request = ScriptCommand.CreateGetErrorsRequest("Assets/Scripts/Player.cs");
        Assert.Equal("Assets/Scripts/Player.cs", request.Parameters!["path"]!.ToString());
    }

    [Fact]
    public void CreateGetErrorsRequest_NoPath_HasEmptyParameters()
    {
        var request = ScriptCommand.CreateGetErrorsRequest();
        Assert.Empty(request.Parameters!);
    }

    // script-find-refs tests
    [Fact]
    public void CreateFindRefsRequest_HasCorrectCommand()
    {
        var request = ScriptCommand.CreateFindRefsRequest("PlayerHealth");
        Assert.Equal(WellKnownCommands.ScriptFindRefs, request.Command);
    }

    [Fact]
    public void CreateFindRefsRequest_SetsSymbol()
    {
        var request = ScriptCommand.CreateFindRefsRequest("PlayerHealth");
        Assert.Equal("PlayerHealth", request.Parameters!["symbol"]!.ToString());
    }

    [Fact]
    public void CreateFindRefsRequest_EmptySymbol_Throws()
    {
        Assert.Throws<ArgumentException>(() => ScriptCommand.CreateFindRefsRequest(""));
    }

    [Fact]
    public void CreateFindRefsRequest_SetsFolder()
    {
        var request = ScriptCommand.CreateFindRefsRequest("Test", "Assets/Scripts");
        Assert.Equal("Assets/Scripts", request.Parameters!["folder"]!.ToString());
    }

    [Fact]
    public void CreateFindRefsRequest_SetsLimit()
    {
        var request = ScriptCommand.CreateFindRefsRequest("Test", limit: 50);
        Assert.Equal(50, (int)request.Parameters!["limit"]!);
    }

    // script-rename-symbol tests
    [Fact]
    public void CreateRenameSymbolRequest_HasCorrectCommand()
    {
        var request = ScriptCommand.CreateRenameSymbolRequest("Old", "New");
        Assert.Equal(WellKnownCommands.ScriptRenameSymbol, request.Command);
    }

    [Fact]
    public void CreateRenameSymbolRequest_SetsNames()
    {
        var request = ScriptCommand.CreateRenameSymbolRequest("OldName", "NewName");
        Assert.Equal("OldName", request.Parameters!["oldName"]!.ToString());
        Assert.Equal("NewName", request.Parameters!["newName"]!.ToString());
    }

    [Fact]
    public void CreateRenameSymbolRequest_EmptyOldName_Throws()
    {
        Assert.Throws<ArgumentException>(() => ScriptCommand.CreateRenameSymbolRequest("", "New"));
    }

    [Fact]
    public void CreateRenameSymbolRequest_EmptyNewName_Throws()
    {
        Assert.Throws<ArgumentException>(() => ScriptCommand.CreateRenameSymbolRequest("Old", ""));
    }

    [Fact]
    public void CreateRenameSymbolRequest_SetsDryRun()
    {
        var request = ScriptCommand.CreateRenameSymbolRequest("Old", "New", dryRun: true);
        Assert.True((bool)request.Parameters!["dryRun"]!);
    }

    [Fact]
    public void CreateRenameSymbolRequest_DryRunFalse_OmitsKey()
    {
        var request = ScriptCommand.CreateRenameSymbolRequest("Old", "New");
        Assert.Null(request.Parameters!["dryRun"]);
    }

    [Fact]
    public async Task EnsureInteractiveEditorReadyAsync_WhenUnlocked_ContinuesWithoutReadyIpc()
    {
        var result = await ScriptCommand.EnsureInteractiveEditorReadyAsync(
            @"C:\project",
            _ => false,
            (_, _) => Task.FromResult(false),
            maxAttempts: 3,
            delayMs: 1);

        Assert.Equal(ScriptInteractiveReadinessResult.ContinueWithoutReadyIpc, result);
    }

    [Fact]
    public async Task EnsureInteractiveEditorReadyAsync_WhenProbeTurnsReady_ReturnsReady()
    {
        var attempts = 0;
        var result = await ScriptCommand.EnsureInteractiveEditorReadyAsync(
            @"C:\project",
            _ => true,
            (_, _) =>
            {
                attempts++;
                return Task.FromResult(attempts >= 2);
            },
            maxAttempts: 3,
            delayMs: 1);

        Assert.Equal(ScriptInteractiveReadinessResult.Ready, result);
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task EnsureInteractiveEditorReadyAsync_WhenStillLockedAndProbeNeverReady_TimesOut()
    {
        var result = await ScriptCommand.EnsureInteractiveEditorReadyAsync(
            @"C:\project",
            _ => true,
            (_, _) => Task.FromResult(false),
            maxAttempts: 2,
            delayMs: 1);

        Assert.Equal(ScriptInteractiveReadinessResult.TimedOut, result);
    }

    [Fact]
    public void CreateInteractiveReadinessFailureResponse_ForGetErrors_IncludesValidateHint()
    {
        var response = ScriptCommand.CreateInteractiveReadinessFailureResponse(
            @"C:\Users\gmdqn\robotapp",
            WellKnownCommands.ScriptGetErrors);

        Assert.Equal(StatusCode.Busy, response.StatusCode);
        Assert.Contains("script get-errors", response.Message);
        Assert.True(response.Data!["requiresIpcReady"]!.GetValue<bool>());
        Assert.Contains("script validate", response.Data["followUpAction"]!.GetValue<string>());
    }
}
