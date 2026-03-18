using System.IO.Pipes;
using System.Text.Json;
using System.Text.Json.Nodes;
using Unityctl.Core.Diagnostics;
using Unityctl.Core.Discovery;
using Unityctl.Core.Platform;
using Unityctl.Shared;

namespace Unityctl.Cli.Commands;

public static class DoctorCommand
{
    public static void Execute(string project, bool json = false)
    {
        var pipeName = Constants.GetPipeName(project);

        // 1. Editor discovery
        var platform = PlatformFactory.Create();
        var discovery = new UnityEditorDiscovery(platform);
        var editors = discovery.FindEditors();
        var editorFound = editors.Count > 0;
        var editorVersion = editors.FirstOrDefault()?.Version ?? "not found";

        // 2. Plugin check
        var manifestPath = Path.Combine(project, "Packages", "manifest.json");
        var pluginInstalled = false;
        if (File.Exists(manifestPath))
        {
            var manifest = File.ReadAllText(manifestPath);
            pluginInstalled = manifest.Contains(Constants.PluginPackageName);
        }

        // 3. IPC probe (1 second timeout)
        var ipcConnected = false;
        try
        {
            using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            pipe.Connect(1000);
            ipcConnected = true;
        }
        catch
        {
            // Connection failed — expected when Unity is not running
        }

        // 4. Editor.log diagnostics
        var logPath = EditorLogDiagnostics.GetEditorLogPath();
        var structured = EditorLogDiagnostics.GetStructuredDiagnostics();
        var humanDiag = EditorLogDiagnostics.GetRecentDiagnostics();

        if (json)
        {
            var results = new JsonObject
            {
                ["editor"] = new JsonObject { ["found"] = editorFound, ["version"] = editorVersion },
                ["plugin"] = new JsonObject { ["installed"] = pluginInstalled },
                ["ipc"] = new JsonObject { ["connected"] = ipcConnected, ["pipeName"] = pipeName }
            };

            if (structured != null)
            {
                var errArr = new JsonArray();
                foreach (var e in structured.Value.Errors)
                    errArr.Add(e);

                var uArr = new JsonArray();
                foreach (var u in structured.Value.UnityctlLines)
                    uArr.Add(u);

                results["editorLog"] = new JsonObject { ["errors"] = errArr, ["unityctl"] = uArr };
            }
            else
            {
                results["editorLog"] = new JsonObject { ["errors"] = new JsonArray(), ["unityctl"] = new JsonArray() };
            }

            results["logPath"] = logPath;

            Console.WriteLine(JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
        }
        else
        {
            Console.WriteLine($"unityctl doctor — project: {project}");
            Console.WriteLine();
            Console.WriteLine(editorFound
                ? $"  \u2713 Unity Editor found: {editorVersion}"
                : "  \u2717 Unity Editor not found");
            Console.WriteLine(pluginInstalled
                ? $"  \u2713 Plugin installed: {Constants.PluginPackageName}"
                : "  \u2717 Plugin not installed (run: unityctl init)");
            Console.WriteLine(ipcConnected
                ? $"  \u2713 IPC connected (pipe: {pipeName})"
                : $"  \u2717 IPC probe failed (pipe: {pipeName})");

            if (humanDiag != null)
            {
                Console.WriteLine();
                Console.Write(humanDiag);
            }
            else if (!ipcConnected)
            {
                Console.WriteLine();
                Console.WriteLine("  No compilation errors in Editor.log");
                Console.WriteLine("  Possible causes: Unity not running, domain reload in progress, or Editor not focused");
            }

            if (logPath != null && humanDiag == null)
            {
                Console.WriteLine($"  Log: {logPath}");
            }
        }

        Environment.ExitCode = ipcConnected ? 0 : 1;
    }
}
