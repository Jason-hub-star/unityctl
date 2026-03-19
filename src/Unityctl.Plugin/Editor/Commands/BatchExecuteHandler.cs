using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class BatchExecuteHandler : CommandHandlerBase
    {
        private static readonly HashSet<string> SupportedCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            WellKnownCommands.GameObjectCreate,
            WellKnownCommands.GameObjectDelete,
            WellKnownCommands.GameObjectSetActive,
            WellKnownCommands.GameObjectMove,
            WellKnownCommands.GameObjectRename,
            WellKnownCommands.ComponentAdd,
            WellKnownCommands.ComponentRemove,
            WellKnownCommands.ComponentSetProperty,
            WellKnownCommands.UiCanvasCreate,
            WellKnownCommands.UiElementCreate,
            WellKnownCommands.UiSetRect,
            WellKnownCommands.MaterialSet,
            WellKnownCommands.MaterialSetShader,
            WellKnownCommands.PlayerSettings,
            WellKnownCommands.ProjectSettingsSet,
            WellKnownCommands.PrefabUnpack,
            WellKnownCommands.AssetCreate,
            WellKnownCommands.AssetCopy,
            WellKnownCommands.AssetMove
        };

        public override string CommandName => WellKnownCommands.BatchExecute;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            if (UnityEngine.Application.isBatchMode)
            {
                return Fail(
                    StatusCode.InvalidParameters,
                    "batch-execute is IPC-only in v1. Transaction rollback depends on live editor Undo state.");
            }

            var commands = request.parameters?["commands"] as JArray;
            if (commands == null || commands.Count == 0)
            {
                return InvalidParameters("Parameter 'commands' must be a non-empty JSON array.");
            }

            var rollbackOnFailure = request.GetParam("rollbackOnFailure", true);
            var validationError = Validate(commands);
            if (validationError != null)
            {
                return validationError;
            }

            var results = new JArray();
            var compensationActions = new List<CompensationAction>();
            var batchGroupName = "unityctl: batch-execute";

            using (var transaction = rollbackOnFailure ? new UndoTransactionScope(batchGroupName) : null)
            {
                for (var i = 0; i < commands.Count; i++)
                {
                    var commandObject = (JObject)commands[i];
                    var childCommand = commandObject.Value<string>("command");
                    var childParameters = commandObject["parameters"] as JObject ?? new JObject();

                    var childRequest = new CommandRequest
                    {
                        command = childCommand,
                        parameters = childParameters,
                        requestId = $"{request.requestId}:{i}"
                    };

                    var childResponse = CommandRegistry.Dispatch(childRequest);
                    results.Add(ToResultEntry(i, childCommand, childResponse));

                    if (rollbackOnFailure && childResponse.success)
                    {
                        if (!BatchCompensation.TryCreateAction(
                                i,
                                childCommand,
                                childResponse.data,
                                out var compensationAction,
                                out var compensationError))
                        {
                            transaction?.Rollback();
                            return Fail(
                                StatusCode.UnknownError,
                                $"Batch execute failed at step {i} ({childCommand}) while recording compensation metadata. Applied rollback.",
                                BuildBatchData(
                                    results,
                                    i,
                                    childCommand,
                                    rollbackOnFailure,
                                    undoRolledBack: true,
                                    compensationResults: null,
                                    compensationFailedAt: i,
                                    compensationRolledBackCount: compensationActions.Count),
                                new List<string> { compensationError });
                        }

                        if (compensationAction != null)
                            compensationActions.Add(compensationAction);

                        UndoTransactionScope.RestoreCurrentGroupName();
                    }

                    if (!childResponse.success)
                    {
                        transaction?.Rollback();
                        var undoRolledBack = rollbackOnFailure;
                        List<CompensationResult> compensationResults = null;
                        int compensationFailedAt = -1;
                        var compensationRolledBack = 0;

                        if (rollbackOnFailure && compensationActions.Count > 0)
                        {
                            BatchCompensation.TryRollback(
                                compensationActions,
                                out compensationResults,
                                out compensationFailedAt);

                            compensationRolledBack = CountSuccessfulCompensations(compensationResults);

                            if (compensationFailedAt >= 0)
                            {
                                return Fail(
                                    StatusCode.UnknownError,
                                    $"Batch execute failed at step {i} ({childCommand}). Undo rollback succeeded, but compensation failed at step {compensationFailedAt}.",
                                    BuildBatchData(
                                        results,
                                        i,
                                        childCommand,
                                        rollbackOnFailure,
                                        undoRolledBack,
                                        compensationResults,
                                        compensationFailedAt,
                                        compensationRolledBack),
                                    childResponse.errors);
                            }
                        }

                        return Fail(
                            (StatusCode)childResponse.statusCode,
                            $"Batch execute failed at step {i} ({childCommand})." +
                            (rollbackOnFailure ? " Applied rollback." : string.Empty),
                            BuildBatchData(
                                results,
                                i,
                                childCommand,
                                rollbackOnFailure,
                                undoRolledBack,
                                compensationResults,
                                compensationFailedAt: null,
                                compensationRolledBackCount: compensationRolledBack),
                            childResponse.errors);
                    }
                }

                transaction?.Complete();
            }

            return Ok(
                $"Batch executed {results.Count} command(s).",
                BuildBatchData(results, null, null, rollbackOnFailure, false, null, null, 0));
#else
            return NotInEditor();
#endif
        }

#if UNITY_EDITOR
        private static CommandResponse Validate(JArray commands)
        {
            for (var i = 0; i < commands.Count; i++)
            {
                if (commands[i] is not JObject commandObject)
                {
                    return InvalidParameters($"commands[{i}] must be an object.");
                }

                var command = commandObject.Value<string>("command");
                if (string.IsNullOrWhiteSpace(command))
                {
                    return InvalidParameters($"commands[{i}].command is required.");
                }

                if (!SupportedCommands.Contains(command))
                {
                    return Fail(
                        StatusCode.InvalidParameters,
                        $"Command '{command}' is not supported by batch-execute v1. " +
                        $"Supported commands: {string.Join(", ", SupportedCommands)}");
                }

                if (commandObject["parameters"] != null && commandObject["parameters"] is not JObject)
                {
                    return InvalidParameters($"commands[{i}].parameters must be an object when provided.");
                }

                var parameters = commandObject["parameters"] as JObject ?? new JObject();
                var compensationValidation = BatchCompensation.ValidateBatchPreconditions(command, parameters);
                if (!string.IsNullOrEmpty(compensationValidation))
                {
                    return Fail(StatusCode.InvalidParameters, compensationValidation);
                }
            }

            return null;
        }

        private static JObject BuildBatchData(
            JArray results,
            int? failedIndex,
            string failedCommand,
            bool rollbackOnFailure,
            bool undoRolledBack,
            IList<CompensationResult> compensationResults,
            int? compensationFailedAt,
            int compensationRolledBackCount)
        {
            var data = new JObject
            {
                ["commandCount"] = results.Count,
                ["results"] = results,
                ["rollbackOnFailure"] = rollbackOnFailure,
                ["rolledBack"] = undoRolledBack || compensationRolledBackCount > 0,
                ["undoRolledBack"] = undoRolledBack,
                ["compensationRolledBackCount"] = compensationRolledBackCount
            };

            if (failedIndex.HasValue)
            {
                data["failedIndex"] = failedIndex.Value;
                data["failedCommand"] = failedCommand;
            }

            if (compensationFailedAt.HasValue)
                data["compensationFailedAt"] = compensationFailedAt.Value;

            if (compensationResults != null)
            {
                var array = new JArray();
                foreach (var result in compensationResults)
                    array.Add(result.ToJson());
                data["compensationResults"] = array;
            }

            return data;
        }

        private static int CountSuccessfulCompensations(IList<CompensationResult> results)
        {
            if (results == null) return 0;

            var count = 0;
            foreach (var result in results)
            {
                if (result.Success) count++;
            }
            return count;
        }

        private static JObject ToResultEntry(int index, string command, CommandResponse response)
        {
            var entry = new JObject
            {
                ["index"] = index,
                ["command"] = command,
                ["statusCode"] = response.statusCode,
                ["success"] = response.success,
                ["message"] = response.message
            };

            if (response.data != null)
            {
                entry["data"] = response.data;
            }

            if (response.errors != null)
            {
                entry["errors"] = JArray.FromObject(response.errors);
            }

            if (!string.IsNullOrEmpty(response.requestId))
            {
                entry["requestId"] = response.requestId;
            }

            return entry;
        }
#endif
    }
}

#if UNITY_EDITOR
    internal sealed class CompensationAction
    {
        public string Kind;
        public string SourcePath;
        public string DestinationPath;
        public string ExpectedGuid;
        public int StepIndex;
    }

    internal sealed class CompensationResult
    {
        public string Kind;
        public bool Success;
        public string Message;
        public string SourcePath;
        public string DestinationPath;
        public string ExpectedGuid;
        public string CurrentGuid;
        public int StepIndex;

        public JObject ToJson()
        {
            var obj = new JObject
            {
                ["kind"] = Kind,
                ["success"] = Success,
                ["message"] = Message,
                ["stepIndex"] = StepIndex
            };

            if (!string.IsNullOrEmpty(SourcePath))
                obj["sourcePath"] = SourcePath;
            if (!string.IsNullOrEmpty(DestinationPath))
                obj["destinationPath"] = DestinationPath;
            if (!string.IsNullOrEmpty(ExpectedGuid))
                obj["expectedGuid"] = ExpectedGuid;
            if (!string.IsNullOrEmpty(CurrentGuid))
                obj["currentGuid"] = CurrentGuid;

            return obj;
        }
    }

    internal static class BatchCompensation
    {
        public static bool TryCreateAction(
            int stepIndex,
            string command,
            JObject responseData,
            out CompensationAction action,
            out string errorMessage)
        {
            action = null;
            errorMessage = null;

            if (responseData == null)
                return true;

            switch (command)
            {
                case WellKnownCommands.AssetCreate:
                    return TryBuildCreate(stepIndex, responseData, out action, out errorMessage);
                case WellKnownCommands.AssetCopy:
                    return TryBuildCopy(stepIndex, responseData, out action, out errorMessage);
                case WellKnownCommands.AssetMove:
                    return TryBuildMove(stepIndex, responseData, out action, out errorMessage);
                default:
                    return true;
            }
        }

        public static bool TryRollback(
            IList<CompensationAction> actions,
            out List<CompensationResult> results,
            out int compensationFailedAt)
        {
            results = new List<CompensationResult>();
            compensationFailedAt = -1;
            var changedAssetDatabase = false;

            for (var i = actions.Count - 1; i >= 0; i--)
            {
                var action = actions[i];
                CompensationResult result;

                switch (action.Kind)
                {
                    case WellKnownCommands.AssetCreate:
                    case WellKnownCommands.AssetCopy:
                        result = RollbackDelete(action);
                        break;
                    case WellKnownCommands.AssetMove:
                        result = RollbackMove(action);
                        break;
                    default:
                        result = new CompensationResult
                        {
                            Kind = action.Kind,
                            Success = false,
                            Message = "Unknown compensation action kind.",
                            StepIndex = action.StepIndex
                        };
                        break;
                }

                results.Add(result);
                if (result.Success && action.Kind == WellKnownCommands.AssetMove)
                    changedAssetDatabase = true;
                if (result.Success && (action.Kind == WellKnownCommands.AssetCreate || action.Kind == WellKnownCommands.AssetCopy))
                    changedAssetDatabase = true;
                if (!result.Success)
                {
                    if (changedAssetDatabase)
                        UnityEditor.AssetDatabase.Refresh();
                    compensationFailedAt = action.StepIndex;
                    return false;
                }
            }

            if (changedAssetDatabase)
                UnityEditor.AssetDatabase.Refresh();

            return true;
        }

        public static string ValidateBatchPreconditions(string command, JObject parameters)
        {
            switch (command)
            {
                case WellKnownCommands.AssetCreate:
                    return ValidateCreate(parameters);
                case WellKnownCommands.AssetCopy:
                    return ValidateCopy(parameters);
                case WellKnownCommands.AssetMove:
                    return ValidateMove(parameters);
                default:
                    return null;
            }
        }

        private static bool TryBuildCreate(
            int stepIndex,
            JObject data,
            out CompensationAction action,
            out string errorMessage)
        {
            action = null;
            errorMessage = null;

            var path = data.Value<string>("path");
            var guid = data.Value<string>("guid");
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(guid))
            {
                errorMessage = "asset-create success response is missing path or guid for compensation.";
                return false;
            }

            action = new CompensationAction
            {
                Kind = WellKnownCommands.AssetCreate,
                DestinationPath = path,
                ExpectedGuid = guid,
                StepIndex = stepIndex
            };

            return true;
        }

        private static bool TryBuildCopy(
            int stepIndex,
            JObject data,
            out CompensationAction action,
            out string errorMessage)
        {
            action = null;
            errorMessage = null;

            var sourcePath = data.Value<string>("sourcePath");
            var path = data.Value<string>("path");
            var guid = data.Value<string>("guid");
            if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(path) || string.IsNullOrEmpty(guid))
            {
                errorMessage = "asset-copy success response is missing sourcePath, path, or guid for compensation.";
                return false;
            }

            action = new CompensationAction
            {
                Kind = WellKnownCommands.AssetCopy,
                SourcePath = sourcePath,
                DestinationPath = path,
                ExpectedGuid = guid,
                StepIndex = stepIndex
            };

            return true;
        }

        private static bool TryBuildMove(
            int stepIndex,
            JObject data,
            out CompensationAction action,
            out string errorMessage)
        {
            action = null;
            errorMessage = null;

            var sourcePath = data.Value<string>("sourcePath");
            var path = data.Value<string>("path");
            var guid = data.Value<string>("guid");
            if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(path) || string.IsNullOrEmpty(guid))
            {
                errorMessage = "asset-move success response is missing sourcePath, path, or guid for compensation.";
                return false;
            }

            action = new CompensationAction
            {
                Kind = WellKnownCommands.AssetMove,
                SourcePath = sourcePath,
                DestinationPath = path,
                ExpectedGuid = guid,
                StepIndex = stepIndex
            };

            return true;
        }

        private static CompensationResult RollbackDelete(CompensationAction action)
        {
            if (!ExistsOnDisk(action.DestinationPath))
            {
                return new CompensationResult
                {
                    Kind = action.Kind,
                    Success = true,
                    Message = "Destination path is already absent.",
                    DestinationPath = action.DestinationPath,
                    ExpectedGuid = action.ExpectedGuid,
                    StepIndex = action.StepIndex
                };
            }

            var currentGuid = UnityEditor.AssetDatabase.AssetPathToGUID(action.DestinationPath);
            if (!string.Equals(currentGuid, action.ExpectedGuid, StringComparison.Ordinal))
            {
                return new CompensationResult
                {
                    Kind = action.Kind,
                    Success = false,
                    Message = "Destination GUID mismatch during compensation delete.",
                    DestinationPath = action.DestinationPath,
                    ExpectedGuid = action.ExpectedGuid,
                    CurrentGuid = currentGuid,
                    StepIndex = action.StepIndex
                };
            }

            var deleted = UnityEditor.AssetDatabase.DeleteAsset(action.DestinationPath);
            return new CompensationResult
            {
                Kind = action.Kind,
                Success = deleted,
                Message = deleted
                    ? "Compensation delete succeeded."
                    : "Compensation delete failed.",
                DestinationPath = action.DestinationPath,
                ExpectedGuid = action.ExpectedGuid,
                CurrentGuid = currentGuid,
                StepIndex = action.StepIndex
            };
        }

        private static CompensationResult RollbackMove(CompensationAction action)
        {
            if (!ExistsOnDisk(action.DestinationPath))
            {
                return new CompensationResult
                {
                    Kind = action.Kind,
                    Success = false,
                    Message = "Destination path is absent during compensation move.",
                    SourcePath = action.SourcePath,
                    DestinationPath = action.DestinationPath,
                    ExpectedGuid = action.ExpectedGuid,
                    StepIndex = action.StepIndex
                };
            }

            var currentGuid = UnityEditor.AssetDatabase.AssetPathToGUID(action.DestinationPath);
            if (!string.Equals(currentGuid, action.ExpectedGuid, StringComparison.Ordinal))
            {
                return new CompensationResult
                {
                    Kind = action.Kind,
                    Success = false,
                    Message = "Destination GUID mismatch during compensation move.",
                    SourcePath = action.SourcePath,
                    DestinationPath = action.DestinationPath,
                    ExpectedGuid = action.ExpectedGuid,
                    CurrentGuid = currentGuid,
                    StepIndex = action.StepIndex
                };
            }

            var sourceGuid = UnityEditor.AssetDatabase.AssetPathToGUID(action.SourcePath);
            if (!string.IsNullOrEmpty(sourceGuid) || ExistsOnDisk(action.SourcePath) || UnityEditor.AssetDatabase.IsValidFolder(action.SourcePath))
            {
                return new CompensationResult
                {
                    Kind = action.Kind,
                    Success = false,
                    Message = "Source path is occupied during compensation move.",
                    SourcePath = action.SourcePath,
                    DestinationPath = action.DestinationPath,
                    ExpectedGuid = action.ExpectedGuid,
                    CurrentGuid = sourceGuid,
                    StepIndex = action.StepIndex
                };
            }

            var moveResult = UnityEditor.AssetDatabase.MoveAsset(action.DestinationPath, action.SourcePath);
            var success = string.IsNullOrEmpty(moveResult);
            return new CompensationResult
            {
                Kind = action.Kind,
                Success = success,
                Message = success
                    ? "Compensation move succeeded."
                    : "Compensation move failed: " + moveResult,
                SourcePath = action.SourcePath,
                DestinationPath = action.DestinationPath,
                ExpectedGuid = action.ExpectedGuid,
                CurrentGuid = currentGuid,
                StepIndex = action.StepIndex
            };
        }

        private static string ValidateCreate(JObject parameters)
        {
            var path = parameters.Value<string>("path");
            if (string.IsNullOrWhiteSpace(path))
                return "asset-create requires a non-empty path.";

            if (ExistsOnDisk(path) || UnityEditor.AssetDatabase.IsValidFolder(path))
                return $"asset-create destination already exists: {path}";

            return null;
        }

        private static string ValidateCopy(JObject parameters)
        {
            var source = parameters.Value<string>("source");
            var destination = parameters.Value<string>("destination");

            if (string.IsNullOrWhiteSpace(source))
                return "asset-copy requires a non-empty source.";
            if (string.IsNullOrWhiteSpace(destination))
                return "asset-copy requires a non-empty destination.";
            if (string.Equals(source, destination, StringComparison.OrdinalIgnoreCase))
                return "asset-copy source and destination must differ.";
            if (!ExistsOnDisk(source) && !UnityEditor.AssetDatabase.IsValidFolder(source))
                return $"asset-copy source not found: {source}";
            if (ExistsOnDisk(destination) || UnityEditor.AssetDatabase.IsValidFolder(destination))
                return $"asset-copy destination already exists: {destination}";

            return null;
        }

        private static string ValidateMove(JObject parameters)
        {
            var source = parameters.Value<string>("source");
            var destination = parameters.Value<string>("destination");

            if (string.IsNullOrWhiteSpace(source))
                return "asset-move requires a non-empty source.";
            if (string.IsNullOrWhiteSpace(destination))
                return "asset-move requires a non-empty destination.";
            if (string.Equals(source, destination, StringComparison.OrdinalIgnoreCase))
                return "asset-move source and destination must differ.";
            if (!ExistsOnDisk(source) && !UnityEditor.AssetDatabase.IsValidFolder(source))
                return $"asset-move source not found: {source}";
            if (ExistsOnDisk(destination) || UnityEditor.AssetDatabase.IsValidFolder(destination))
                return $"asset-move destination already exists: {destination}";

            return null;
        }

        private static bool ExistsOnDisk(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return false;

            var fullPath = System.IO.Path.GetFullPath(
                System.IO.Path.Combine(UnityEngine.Application.dataPath, "..", assetPath.Replace('/', System.IO.Path.DirectorySeparatorChar)));
            return System.IO.File.Exists(fullPath) || System.IO.Directory.Exists(fullPath);
        }
    }
#endif
