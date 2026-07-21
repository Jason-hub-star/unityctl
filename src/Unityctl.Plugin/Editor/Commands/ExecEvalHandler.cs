#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    /// <summary>
    /// Handles "exec-eval": compiles multi-statement C# with the Unity-bundled
    /// Roslyn compiler (csc) and executes it in the editor AppDomain — no domain
    /// reload. Each call loads one throwaway assembly that stays until the next
    /// reload; acceptable for a dev tool, same trade-off as scripting REPLs.
    ///
    /// Security: full-trust code execution. Disabled unless
    /// ProjectSettings/UnityctlSettings.asset sets "AllowEval": true — pattern
    /// blocking cannot contain compiled code, so the gate is all-or-nothing.
    /// </summary>
    public class ExecEvalHandler : CommandHandlerBase
    {
        private const int CompileTimeoutMs = 30000;
        private const int MaxDiagnosticLines = 20;

        public override string CommandName => WellKnownCommands.ExecEval;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var settings = UnityctlProjectSettingsStore.LoadCurrent();
            if (settings == null || !settings.AllowEval)
            {
                return Fail(StatusCode.InvalidParameters,
                    "exec eval is disabled. Set \"AllowEval\": true in ProjectSettings/UnityctlSettings.asset to enable full-trust C# evaluation.");
            }

            var code = request.GetParam("code");
            if (string.IsNullOrWhiteSpace(code))
                return InvalidParameters("'code' parameter is required.");

            var compilerPaths = ResolveCompilerPaths();
            if (compilerPaths == null)
            {
                return Fail(StatusCode.UnknownError,
                    "Bundled Roslyn compiler not found under EditorApplication.applicationContentsPath.");
            }

            var workDir = Path.Combine(Path.GetTempPath(), "unityctl-eval", Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(workDir);

                var className = "UnityctlEval_" + Guid.NewGuid().ToString("N");
                var sourcePath = Path.Combine(workDir, "eval.cs");
                var dllPath = Path.Combine(workDir, "eval.dll");
                File.WriteAllText(sourcePath, BuildSource(className, code));

                var rspPath = Path.Combine(workDir, "eval.rsp");
                File.WriteAllText(rspPath, BuildResponseFile(dllPath, sourcePath));

                var compileWatch = Stopwatch.StartNew();
                var compile = RunCompiler(compilerPaths.Value, rspPath);
                compileWatch.Stop();

                if (compile.TimedOut)
                    return Fail(StatusCode.UnknownError, $"Compile timed out after {CompileTimeoutMs}ms.");

                if (compile.ExitCode != 0)
                {
                    var diagnostics = TrimDiagnostics(compile.Output);
                    return Fail(
                        StatusCode.InvalidParameters,
                        "Compile failed.",
                        new JObject { ["diagnostics"] = diagnostics });
                }

                var executeWatch = Stopwatch.StartNew();
                object result;
                try
                {
                    var assembly = Assembly.Load(File.ReadAllBytes(dllPath));
                    var method = assembly.GetType(className)?.GetMethod("Run", BindingFlags.Public | BindingFlags.Static);
                    if (method == null)
                        return Fail(StatusCode.UnknownError, "Compiled assembly is missing the eval entry point.");

                    result = method.Invoke(null, null);
                }
                catch (TargetInvocationException ex)
                {
                    var inner = ex.InnerException ?? ex;
                    return Fail(
                        StatusCode.UnknownError,
                        $"Execution error: {inner.GetType().Name}: {inner.Message}",
                        new JObject { ["exceptionType"] = inner.GetType().FullName },
                        string.IsNullOrWhiteSpace(inner.StackTrace)
                            ? null
                            : new List<string> { inner.StackTrace });
                }
                executeWatch.Stop();

                var data = new JObject
                {
                    ["result"] = SerializeResult(result),
                    ["compileMs"] = compileWatch.ElapsedMilliseconds,
                    ["executeMs"] = executeWatch.ElapsedMilliseconds
                };
                return Ok("eval completed", data);
            }
            catch (Exception ex)
            {
                return Fail(StatusCode.UnknownError, $"eval error: {ex.Message}");
            }
            finally
            {
                try { Directory.Delete(workDir, recursive: true); } catch { }
            }
        }

        private static string BuildSource(string className, string code)
        {
            // Trailing "return null;" lets statement-only snippets compile; a user
            // "return x;" above it just makes it unreachable (warning suppressed).
            return new StringBuilder()
                .AppendLine("#pragma warning disable 0162, 0168, 0219")
                .AppendLine("using System;")
                .AppendLine("using System.Collections.Generic;")
                .AppendLine("using System.Linq;")
                .AppendLine("using UnityEngine;")
                .AppendLine("using UnityEditor;")
                .AppendLine()
                .AppendLine($"public static class {className}")
                .AppendLine("{")
                .AppendLine("    public static object Run()")
                .AppendLine("    {")
                .AppendLine(code)
                .AppendLine("        return null;")
                .AppendLine("    }")
                .AppendLine("}")
                .ToString();
        }

        private static string BuildResponseFile(string dllPath, string sourcePath)
        {
            // Reference every non-dynamic assembly loaded in the editor AppDomain
            // (deduped by simple name) so eval code sees the same API surface as
            // project scripts. -nostdlib+ because mscorlib comes from that set.
            var references = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic) continue;
                string location;
                try { location = assembly.Location; }
                catch { continue; }
                if (string.IsNullOrEmpty(location) || !File.Exists(location)) continue;

                var name = Path.GetFileNameWithoutExtension(location);
                if (!references.ContainsKey(name))
                    references[name] = location;
            }

            var rsp = new StringBuilder()
                .AppendLine("-nologo")
                .AppendLine("-noconfig")
                .AppendLine("-nostdlib+")
                .AppendLine("-t:library")
                .AppendLine("-nowarn:1701,1702")
                .AppendLine($"-out:\"{dllPath}\"");
            foreach (var reference in references.Values)
                rsp.AppendLine($"-r:\"{reference}\"");
            rsp.AppendLine($"\"{sourcePath}\"");
            return rsp.ToString();
        }

        private static (string DotnetPath, string CscPath)? ResolveCompilerPaths()
        {
            var contents = UnityEditor.EditorApplication.applicationContentsPath;
            var cscPath = Path.Combine(contents, "DotNetSdkRoslyn", "csc.dll");
            var dotnetPath = Path.Combine(contents, "NetCoreRuntime", "dotnet");
            if (!File.Exists(dotnetPath))
                dotnetPath += ".exe";

            if (!File.Exists(cscPath) || !File.Exists(dotnetPath))
                return null;

            return (dotnetPath, cscPath);
        }

        private static (int ExitCode, string Output, bool TimedOut) RunCompiler(
            (string DotnetPath, string CscPath) paths, string rspPath)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = paths.DotnetPath,
                Arguments = $"\"{paths.CscPath}\" @\"{rspPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                var stderrTask = process.StandardError.ReadToEndAsync();
                var stdout = process.StandardOutput.ReadToEnd();

                if (!process.WaitForExit(CompileTimeoutMs))
                {
                    try { process.Kill(); } catch { }
                    return (-1, string.Empty, true);
                }

                return (process.ExitCode, stdout + stderrTask.Result, false);
            }
        }

        private static JArray TrimDiagnostics(string compilerOutput)
        {
            var lines = (compilerOutput ?? string.Empty)
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Take(MaxDiagnosticLines);
            return new JArray(lines.Cast<object>().ToArray());
        }

        private static JToken SerializeResult(object result)
        {
            if (result == null)
                return JValue.CreateNull();

            try
            {
                return JToken.FromObject(result);
            }
            catch
            {
                // UnityEngine.Object graphs can self-reference or throw on
                // serialization — fall back to the display string.
                return new JValue(result.ToString());
            }
        }
    }
}
#endif
