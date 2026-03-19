using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class BuildHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.Build;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var target = request.GetParam("target", "StandaloneWindows64");
            var outputPath = request.GetParam("outputPath", null);
            var dryRun = request.GetParam<bool>("dryRun");

            if (!BuildProfileUtility.TryParseBuildTarget(target, out var buildTarget, out var canonicalTarget))
            {
                return InvalidParameters(
                    $"Unknown build target: {target}. Valid targets: {BuildProfileUtility.SupportedTargetList}");
            }

            if (dryRun)
            {
                var checks = RunPreflight(canonicalTarget, buildTarget, outputPath);
                var data = new JObject
                {
                    ["checks"] = JArray.FromObject(checks)
                };
                var hasErrors = checks.Any(c => c.category == "error" && !c.passed);
                return hasErrors
                    ? Fail(StatusCode.BuildFailed, "Preflight failed", data: data)
                    : Ok("Preflight passed", data);
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                outputPath = Path.Combine("Builds", canonicalTarget, GetDefaultExecutableName(buildTarget));
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
                target = buildTarget,
                options = UnityEditor.BuildOptions.None
            };

            UnityEngine.Debug.Log($"[unityctl] Building {canonicalTarget} → {outputPath} ({scenes.Length} scenes)");

            var report = UnityEditor.BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                var buildData = new JObject
                {
                    ["outputPath"] = outputPath,
                    ["totalSize"] = (long)summary.totalSize,
                    ["totalTime"] = summary.totalTime.TotalSeconds,
                    ["totalErrors"] = summary.totalErrors,
                    ["totalWarnings"] = summary.totalWarnings
                };
                return Ok("Build succeeded", buildData);
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

        private List<PreflightCheck> RunPreflight(string targetName, UnityEditor.BuildTarget buildTarget, string outputPath)
        {
            var checks = new List<PreflightCheck>();

            // === Error: BuildTarget valid ===
            checks.Add(new PreflightCheck
            {
                category = "error",
                check = "BuildTarget",
                passed = true,
                message = $"Target '{targetName}' is valid"
            });

            // === Error: Platform module installed ===
            var buildTargetGroup = UnityEditor.BuildPipeline.GetBuildTargetGroup(buildTarget);
            var moduleInstalled = UnityEditor.BuildPipeline.IsBuildTargetSupported(buildTargetGroup, buildTarget);
            checks.Add(new PreflightCheck
            {
                category = "error",
                check = "PlatformModule",
                passed = moduleInstalled,
                message = moduleInstalled
                    ? $"Platform module installed for {targetName}"
                    : $"Platform module not installed for {targetName}"
            });

            // === Error: Scenes exist ===
            var enabledScenes = UnityEditor.EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .ToArray();

            checks.Add(new PreflightCheck
            {
                category = "error",
                check = "ScenesExist",
                passed = enabledScenes.Length > 0,
                message = enabledScenes.Length > 0
                    ? $"{enabledScenes.Length} scene(s) enabled in Build Settings"
                    : "No scenes enabled in Build Settings"
            });

            // === Error: Scene files on disk ===
            foreach (var scene in enabledScenes)
            {
                var exists = File.Exists(scene.path);
                checks.Add(new PreflightCheck
                {
                    category = "error",
                    check = "SceneFile",
                    passed = exists,
                    message = exists
                        ? $"Scene file found: {scene.path}"
                        : $"Scene file missing: {scene.path}",
                    details = scene.path
                });
            }

            // === Error: Script compilation ===
            var compilationFailed = UnityEditor.EditorUtility.scriptCompilationFailed;
            checks.Add(new PreflightCheck
            {
                category = "error",
                check = "Compilation",
                passed = !compilationFailed,
                message = compilationFailed
                    ? "Script compilation errors detected"
                    : "Scripts compiled successfully"
            });

            // === Error: Output path writable ===
            var resolvedOutput = string.IsNullOrEmpty(outputPath)
                ? Path.Combine("Builds", targetName, GetDefaultExecutableName(buildTarget))
                : outputPath;
            var parentDir = Path.GetDirectoryName(resolvedOutput);
            var outputWritable = ValidateOutputPath(parentDir);
            checks.Add(new PreflightCheck
            {
                category = "error",
                check = "OutputPath",
                passed = outputWritable,
                message = outputWritable
                    ? $"Output path is writable: {resolvedOutput}"
                    : $"Output path is not writable: {resolvedOutput}",
                details = resolvedOutput
            });

            // === Warning: Active build target mismatch ===
            var activeBuildTarget = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
            var targetMismatch = activeBuildTarget != buildTarget;
            checks.Add(new PreflightCheck
            {
                category = "warning",
                check = "ActiveTargetMismatch",
                passed = !targetMismatch,
                message = targetMismatch
                    ? $"Active target is {activeBuildTarget}, build target is {buildTarget}"
                    : $"Active target matches build target ({buildTarget})"
            });

            // === Warning: Android SDK/NDK ===
            if (buildTarget == UnityEditor.BuildTarget.Android)
            {
                var sdkRoot = UnityEditor.EditorPrefs.GetString("AndroidSdkRoot");
                var sdkFound = !string.IsNullOrEmpty(sdkRoot) && Directory.Exists(sdkRoot);
                checks.Add(new PreflightCheck
                {
                    category = "warning",
                    check = "AndroidSdk",
                    passed = sdkFound,
                    message = sdkFound
                        ? $"Android SDK found at: {sdkRoot}"
                        : "Android SDK path not found or does not exist",
                    details = sdkRoot
                });
            }

            // === Warning: Currently compiling ===
            var isCompiling = UnityEditor.EditorApplication.isCompiling;
            checks.Add(new PreflightCheck
            {
                category = "warning",
                check = "IsCompiling",
                passed = !isCompiling,
                message = isCompiling
                    ? "Scripts are currently compiling"
                    : "No active compilation"
            });

            // === Info: Scene list ===
            var scenePaths = enabledScenes.Select(s => s.path).ToArray();
            checks.Add(new PreflightCheck
            {
                category = "info",
                check = "SceneList",
                passed = true,
                message = $"Enabled scenes: {string.Join(", ", scenePaths)}",
                details = string.Join(";", scenePaths)
            });

            // === Info: Scripting backend ===
            var selectedGroup = UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup;
            var backend = UnityEditor.PlayerSettings.GetScriptingBackend(selectedGroup);
            checks.Add(new PreflightCheck
            {
                category = "info",
                check = "ScriptingBackend",
                passed = true,
                message = backend.ToString()
            });

            // === Info: Define symbols ===
            var defines = UnityEditor.PlayerSettings.GetScriptingDefineSymbolsForGroup(selectedGroup);
            checks.Add(new PreflightCheck
            {
                category = "info",
                check = "DefineSymbols",
                passed = true,
                message = string.IsNullOrEmpty(defines) ? "(none)" : defines
            });

            // === Info: PlayerSettings summary ===
            checks.Add(new PreflightCheck
            {
                category = "info",
                check = "PlayerSettings",
                passed = true,
                message = $"{UnityEngine.Application.productName} v{UnityEngine.Application.version}"
            });

            return checks;
        }

        private static bool ValidateOutputPath(string parentDir)
        {
            if (string.IsNullOrEmpty(parentDir))
                return true;

            if (Directory.Exists(parentDir))
                return true;

            // Check if we could create the directory (parent exists)
            var grandParent = Path.GetDirectoryName(parentDir);
            if (string.IsNullOrEmpty(grandParent))
                return true;

            return Directory.Exists(grandParent);
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
