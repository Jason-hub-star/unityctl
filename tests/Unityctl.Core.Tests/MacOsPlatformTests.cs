using Unityctl.Core.Platform;
using Xunit;

namespace Unityctl.Core.Tests;

public sealed class MacOsPlatformTests
{
    [Fact]
    public void ParseProcessList_DistinguishesInteractiveEditorFromAssetWorkers()
    {
        const string processList = """
             93832 /Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -projectPath /Users/jason/My Unity Project -logFile /tmp/editor.log
             94238 /Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -adb2 -batchMode -projectPath /Users/jason/My Unity Project -logFile Logs/AssetImportWorker0.log
             94239 /Applications/Other.app/Contents/MacOS/Other -projectPath /Users/jason/My Unity Project
            """;

        var processes = MacOsPlatform.ParseProcessList(processList);

        Assert.Equal(2, processes.Count);
        Assert.Equal(93832, processes[0].ProcessId);
        Assert.Equal("/Users/jason/My Unity Project", processes[0].ProjectPath);
        Assert.Equal("6000.3.16f1", processes[0].Version);
        Assert.True(processes[0].IsInteractiveEditor);
        Assert.True(processes[1].IsBatchMode);
        Assert.False(processes[1].IsInteractiveEditor);
    }
}
