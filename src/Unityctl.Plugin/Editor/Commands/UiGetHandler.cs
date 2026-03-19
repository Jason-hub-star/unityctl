using System;
using Newtonsoft.Json.Linq;
using UnityEditor.SceneManagement;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class UiGetHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.UiGet;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var id = request.GetParam("id", null);
            if (string.IsNullOrEmpty(id))
            {
                return InvalidParameters("Parameter 'id' is required.");
            }

            var gameObject = GlobalObjectIdResolver.Resolve<UnityEngine.GameObject>(id);
            if (gameObject == null)
            {
                return Fail(StatusCode.NotFound, $"GameObject not found: {id}");
            }

            if (!UiReadUtility.IsUiGameObject(gameObject))
            {
                return Fail(StatusCode.NotFound, $"UI element not found: {id}");
            }

            var data = UiReadUtility.CreateUiDetails(gameObject, id);
            return Ok($"UI element '{gameObject.name}'", data);
#else
            return NotInEditor();
#endif
        }
    }

    public class UiToggleHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.UiToggle;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var id = request.GetParam("id", null);
            if (string.IsNullOrEmpty(id))
            {
                return InvalidParameters("Parameter 'id' is required.");
            }

            if (!UiInteractionCommandHelper.TryGetBool(request, "value", out var targetValue))
            {
                return InvalidParameters("Parameter 'value' must be provided as true or false.");
            }

            var requestedMode = request.GetParam("mode", "auto");
            if (!UiInteractionCommandHelper.TryResolveMode(requestedMode, out var effectiveMode, out var modeFailure))
            {
                return modeFailure;
            }

            if (!UiInteractionCommandHelper.TryResolveUiComponent<UnityEngine.UI.Toggle>(id, "Toggle", out var toggle, out var failure))
            {
                return failure;
            }

            return effectiveMode == "play"
                ? ApplyPlay(toggle, id, targetValue, requestedMode, effectiveMode)
                : ApplyEdit(toggle, id, targetValue, requestedMode, effectiveMode);
#else
            return NotInEditor();
#endif
        }

#if UNITY_EDITOR
        private static CommandResponse ApplyEdit(UnityEngine.UI.Toggle toggle, string id, bool targetValue, string requestedMode, string effectiveMode)
        {
            var prefabReject = PrefabGuard.RejectIfPrefab(toggle);
            if (prefabReject != null)
            {
                return prefabReject;
            }

            var undoName = $"unityctl: ui-toggle: {toggle.gameObject.name}";
            using (new UndoScope(undoName))
            {
                UnityEditor.Undo.RecordObject(toggle, undoName);

                var previousValue = toggle.isOn;
                toggle.SetIsOnWithoutNotify(targetValue);
                UnityEditor.EditorUtility.SetDirty(toggle);
                EditorSceneManager.MarkSceneDirty(toggle.gameObject.scene);

                return Ok($"Toggle '{toggle.gameObject.name}' set to {toggle.isOn}", CreateTogglePayload(toggle, id, previousValue, requestedMode, effectiveMode, sceneDirty: true));
            }
        }

        private static CommandResponse ApplyPlay(UnityEngine.UI.Toggle toggle, string id, bool targetValue, string requestedMode, string effectiveMode)
        {
            var previousValue = toggle.isOn;
            toggle.SetIsOnWithoutNotify(targetValue);
            return Ok($"Toggle '{toggle.gameObject.name}' set to {toggle.isOn}", CreateTogglePayload(toggle, id, previousValue, requestedMode, effectiveMode, sceneDirty: false));
        }

        private static JObject CreateTogglePayload(UnityEngine.UI.Toggle toggle, string id, bool previousValue, string requestedMode, string effectiveMode, bool sceneDirty)
        {
            return new JObject
            {
                ["globalObjectId"] = id,
                ["componentGlobalObjectId"] = GlobalObjectIdResolver.GetId(toggle),
                ["gameObjectName"] = toggle.gameObject.name,
                ["uiType"] = "Toggle",
                ["requestedMode"] = requestedMode,
                ["modeApplied"] = effectiveMode,
                ["previousValue"] = previousValue,
                ["currentValue"] = toggle.isOn,
                ["eventsTriggered"] = false,
                ["scenePath"] = toggle.gameObject.scene.path,
                ["sceneDirty"] = sceneDirty
            };
        }
#endif
    }

    public class UiInputHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.UiInput;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var id = request.GetParam("id", null);
            if (string.IsNullOrEmpty(id))
            {
                return InvalidParameters("Parameter 'id' is required.");
            }

            var text = request.GetParam("text", null);
            if (text == null)
            {
                return InvalidParameters("Parameter 'text' is required.");
            }

            var requestedMode = request.GetParam("mode", "auto");
            if (!UiInteractionCommandHelper.TryResolveMode(requestedMode, out var effectiveMode, out var modeFailure))
            {
                return modeFailure;
            }

            if (!UiInteractionCommandHelper.TryResolveUiComponent<UnityEngine.UI.InputField>(id, "InputField", out var inputField, out var failure))
            {
                return failure;
            }

            return effectiveMode == "play"
                ? ApplyPlay(inputField, id, text, requestedMode, effectiveMode)
                : ApplyEdit(inputField, id, text, requestedMode, effectiveMode);
#else
            return NotInEditor();
#endif
        }

#if UNITY_EDITOR
        private static CommandResponse ApplyEdit(UnityEngine.UI.InputField inputField, string id, string text, string requestedMode, string effectiveMode)
        {
            var prefabReject = PrefabGuard.RejectIfPrefab(inputField);
            if (prefabReject != null)
            {
                return prefabReject;
            }

            var undoName = $"unityctl: ui-input: {inputField.gameObject.name}";
            using (new UndoScope(undoName))
            {
                UnityEditor.Undo.RecordObject(inputField, undoName);

                var previousText = inputField.text;
                inputField.SetTextWithoutNotify(text);
                inputField.ForceLabelUpdate();
                UnityEditor.EditorUtility.SetDirty(inputField);
                EditorSceneManager.MarkSceneDirty(inputField.gameObject.scene);

                return Ok($"InputField '{inputField.gameObject.name}' text updated", CreateInputPayload(inputField, id, previousText, requestedMode, effectiveMode, sceneDirty: true));
            }
        }

        private static CommandResponse ApplyPlay(UnityEngine.UI.InputField inputField, string id, string text, string requestedMode, string effectiveMode)
        {
            var previousText = inputField.text;
            inputField.SetTextWithoutNotify(text);
            inputField.ForceLabelUpdate();
            return Ok($"InputField '{inputField.gameObject.name}' text updated", CreateInputPayload(inputField, id, previousText, requestedMode, effectiveMode, sceneDirty: false));
        }

        private static JObject CreateInputPayload(UnityEngine.UI.InputField inputField, string id, string previousText, string requestedMode, string effectiveMode, bool sceneDirty)
        {
            return new JObject
            {
                ["globalObjectId"] = id,
                ["componentGlobalObjectId"] = GlobalObjectIdResolver.GetId(inputField),
                ["gameObjectName"] = inputField.gameObject.name,
                ["uiType"] = "InputField",
                ["requestedMode"] = requestedMode,
                ["modeApplied"] = effectiveMode,
                ["previousText"] = previousText,
                ["currentText"] = inputField.text,
                ["eventsTriggered"] = false,
                ["scenePath"] = inputField.gameObject.scene.path,
                ["sceneDirty"] = sceneDirty
            };
        }
#endif
    }

#if UNITY_EDITOR
    internal static class UiInteractionCommandHelper
    {
        public static bool TryResolveMode(string requestedMode, out string effectiveMode, out CommandResponse failure)
        {
            effectiveMode = string.Empty;
            failure = null;

            var normalized = string.IsNullOrWhiteSpace(requestedMode)
                ? "auto"
                : requestedMode.Trim().ToLowerInvariant();

            switch (normalized)
            {
                case "auto":
                    effectiveMode = UnityEditor.EditorApplication.isPlaying ? "play" : "edit";
                    return true;
                case "edit":
                    if (UnityEditor.EditorApplication.isPlaying)
                    {
                        failure = CommandResponse.Fail(
                            StatusCode.InvalidParameters,
                            "Mode 'edit' is unavailable while the Unity Editor is in Play Mode. Stop Play Mode or use --mode play/auto.");
                        return false;
                    }

                    effectiveMode = "edit";
                    return true;
                case "play":
                    if (!UnityEditor.EditorApplication.isPlaying)
                    {
                        failure = CommandResponse.Fail(
                            StatusCode.InvalidParameters,
                            "Mode 'play' requires the Unity Editor to already be in Play Mode. Start Play Mode first or use --mode auto/edit.");
                        return false;
                    }

                    effectiveMode = "play";
                    return true;
                default:
                    failure = CommandResponse.Fail(
                        StatusCode.InvalidParameters,
                        "Parameter 'mode' must be one of: auto, edit, play.");
                    return false;
            }
        }

        public static bool TryResolveUiComponent<T>(string id, string expectedUiType, out T component, out CommandResponse failure)
            where T : UnityEngine.Component
        {
            component = null;
            failure = null;

            var gameObject = GlobalObjectIdResolver.Resolve<UnityEngine.GameObject>(id);
            if (gameObject == null)
            {
                failure = CommandResponse.Fail(StatusCode.NotFound, $"GameObject not found: {id}");
                return false;
            }

            if (!UiReadUtility.IsUiGameObject(gameObject))
            {
                failure = CommandResponse.Fail(StatusCode.NotFound, $"UI element not found: {id}");
                return false;
            }

            component = gameObject.GetComponent<T>();
            if (component == null)
            {
                failure = CommandResponse.Fail(StatusCode.InvalidParameters, $"UI element '{gameObject.name}' is not a {expectedUiType}.");
                return false;
            }

            return true;
        }

        public static bool TryGetBool(CommandRequest request, string key, out bool value)
        {
            value = false;

            if (request.parameters == null || request.parameters[key] == null || request.parameters[key].Type == JTokenType.Null)
            {
                return false;
            }

            if (request.parameters[key].Type == JTokenType.Boolean)
            {
                value = request.parameters[key].Value<bool>();
                return true;
            }

            if (request.parameters[key].Type == JTokenType.String
                && bool.TryParse(request.parameters[key].Value<string>(), out var parsed))
            {
                value = parsed;
                return true;
            }

            return false;
        }
    }
#endif
}
