using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class BuildHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.Build;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var target = request.GetParam("target", "StandaloneWindows64");
            var outputPath = request.GetParam("outputPath", null);

            var buildTarget = ParseBuildTarget(target);
            if (buildTarget == null)
            {
                return InvalidParameters(
                    $"Unknown build target: {target}. Valid targets: StandaloneWindows64, StandaloneOSX, StandaloneLinux64, Android, iOS, WebGL");
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                outputPath = Path.Combine("Builds", target, GetDefaultExecutableName(buildTarget.Value));
            }

            var scenes = UnityEditor.EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                return InvalidParameters(
                    "No scenes enabled in Build Settings. Add scenes to EditorBuildSettings.");
            }

            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var options = new UnityEditor.BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = buildTarget.Value,
                options = UnityEditor.BuildOptions.None
            };

            UnityEngine.Debug.Log($"[unityctl] Building {target} → {outputPath} ({scenes.Length} scenes)");

            var report = UnityEditor.BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                var data = new JObject
                {
                    ["outputPath"] = outputPath,
                    ["totalSize"] = (long)summary.totalSize,
                    ["totalTime"] = summary.totalTime.TotalSeconds,
                    ["totalErrors"] = summary.totalErrors,
                    ["totalWarnings"] = summary.totalWarnings
                };
                return Ok("Build succeeded", data);
            }

            var errors = new List<string>();
            foreach (var step in report.steps)
            {
                foreach (var msg in step.messages)
                {
                    if (msg.type == UnityEngine.LogType.Error)
                    {
                        errors.Add(msg.content);
                    }
                }
            }

            return Fail(
                StatusCode.BuildFailed,
                $"Build failed: {summary.result}",
                errors: errors);
        }

        protected override CommandResponse HandleException(Exception exception)
        {
            return Fail(
                StatusCode.UnknownError,
                $"Build exception: {exception.Message}",
                errors: GetStackTrace(exception));
        }

        private static UnityEditor.BuildTarget? ParseBuildTarget(string target)
        {
            return target?.ToLowerInvariant() switch
            {
                "standalonewindows64" or "win64" => UnityEditor.BuildTarget.StandaloneWindows64,
                "standalonewindows" or "win32" => UnityEditor.BuildTarget.StandaloneWindows,
                "standaloneosx" or "macos" => UnityEditor.BuildTarget.StandaloneOSX,
                "standalonelinux64" or "linux64" => UnityEditor.BuildTarget.StandaloneLinux64,
                "android" => UnityEditor.BuildTarget.Android,
                "ios" => UnityEditor.BuildTarget.iOS,
                "webgl" => UnityEditor.BuildTarget.WebGL,
                _ => null
            };
        }

        private static string GetDefaultExecutableName(UnityEditor.BuildTarget target)
        {
            return target switch
            {
                UnityEditor.BuildTarget.StandaloneWindows64 => "Game.exe",
                UnityEditor.BuildTarget.StandaloneWindows => "Game.exe",
                UnityEditor.BuildTarget.StandaloneOSX => "Game.app",
                UnityEditor.BuildTarget.StandaloneLinux64 => "Game.x86_64",
                UnityEditor.BuildTarget.Android => "Game.apk",
                UnityEditor.BuildTarget.WebGL => "index.html",
                _ => "Game"
            };
        }
    }
}
