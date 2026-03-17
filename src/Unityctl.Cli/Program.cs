using ConsoleAppFramework;

var app = ConsoleApp.Create();
app.Add("", () => Console.WriteLine($"unityctl v{Unityctl.Shared.Constants.Version} — Unity CLI for agentic workflows"));
app.Run(args);
