using ConsoleAppFramework;
using Unityctl.Cli.Commands;

var app = ConsoleApp.Create();

app.Add("", () => Console.WriteLine($"unityctl v{Unityctl.Shared.Constants.Version} — Unity CLI for agentic workflows\nUse --help for available commands."));

app.Add("init", (string project, string? source = null) => InitCommand.Execute(project, source));

app.Add("editor list", () => EditorCommands.List());

app.Add("status", (string project, bool wait = false) => StatusCommand.Execute(project, wait));

app.Add("build", (string project, string target = "StandaloneWindows64", string? output = null) => BuildCommand.Execute(project, target, output));

app.Add("test", (string project, string mode = "edit", string? filter = null) => TestCommand.Execute(project, mode, filter));

app.Add("check", (string project, string type = "compile") => CheckCommand.Execute(project, type));

app.Run(args);
