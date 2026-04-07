#if UNITY_EDITOR
using System;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class UitkClickHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.UitkClick;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            if (UitkElementResolver.FindUidocumentType() == null)
                return Fail(StatusCode.NotFound, "UI Toolkit (UIDocument) not available in this Unity version.");

            var name = request.GetParam("name", null);
            var locator = request.GetParam("locator", null);
            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(locator))
                return InvalidParameters("Parameter 'name' or 'locator' is required.");

            var requestedMode = request.GetParam("mode", "auto");
            if (!UiInteractionCommandHelper.TryResolveMode(requestedMode, out var effectiveMode, out var modeFailure))
                return modeFailure;

            if (!string.Equals(effectiveMode, "play", StringComparison.OrdinalIgnoreCase))
            {
                return Fail(
                    StatusCode.InvalidParameters,
                    "`uitk click` currently requires Play Mode. Start Play Mode first or use `--mode play` once the Editor is already playing.");
            }

            if (!UitkElementResolver.TryResolveSingle(name, locator, out var resolved, out var candidates, out var ambiguous))
            {
                if (ambiguous)
                {
                    return Fail(
                        StatusCode.InvalidParameters,
                        "Multiple UI Toolkit elements matched the query. Retry with locator.",
                        new JObject { ["candidates"] = candidates });
                }

                return Fail(StatusCode.NotFound, $"UI Toolkit element not found for name='{name}' locator='{locator}'.");
            }

            if (!GetBoolProp(resolved.Element, "visible", defaultValue: true))
                return Fail(StatusCode.InvalidParameters, $"UI Toolkit element '{resolved.Name}' is not visible.");

            if (!GetBoolProp(resolved.Element, "enabledInHierarchy", defaultValue: true))
                return Fail(StatusCode.InvalidParameters, $"UI Toolkit element '{resolved.Name}' is disabled in hierarchy.");

            if (!TryInvokeClick(resolved.Element, out var invokeMethod, out var invokeFailure))
                return Fail(StatusCode.InvalidParameters, invokeFailure);

            var data = UitkElementResolver.ToSummary(resolved);
            data["requestedMode"] = requestedMode;
            data["modeApplied"] = effectiveMode;
            data["eventsTriggered"] = true;
            data["invokeMethod"] = invokeMethod;

            return Ok($"Clicked UI Toolkit element '{resolved.Name}'", data);
        }

        private static bool TryInvokeClick(object element, out string invokeMethod, out string failure)
        {
            invokeMethod = string.Empty;
            failure = string.Empty;

            var elementType = element.GetType();
            if (TryDispatchClickEvent(element, out invokeMethod, out var dispatchError))
                return true;

            var clickableProp = elementType.GetProperty("clickable", BindingFlags.Public | BindingFlags.Instance);
            var clickable = clickableProp?.GetValue(element);
            if (clickable == null)
            {
                failure =
                    $"Element '{GetElementName(element)}' ({elementType.Name}) is not clickable. `uitk click` currently supports Button-like elements with a Clickable manipulator.";
                return false;
            }

            if (TryInvokeClickableMethod(clickable, "Invoke", out invokeMethod, out var invokeError))
                return true;

            if (TryInvokeClickableMethod(clickable, "SimulateSingleClick", out invokeMethod, out var simulateError))
                return true;

            var detail = dispatchError ?? simulateError ?? invokeError;
            failure = detail == null
                ? $"Element '{GetElementName(element)}' ({elementType.Name}) exposes a Clickable manipulator, but unityctl could not trigger it via reflection."
                : $"Element '{GetElementName(element)}' ({elementType.Name}) could not be clicked via reflection: {detail.Message}";
            return false;
        }

        private static bool TryDispatchClickEvent(object element, out string invokeMethod, out Exception error)
        {
            invokeMethod = string.Empty;
            error = null;

            var assembly = element.GetType().Assembly;
            var clickEventType = assembly.GetType("UnityEngine.UIElements.ClickEvent");
            var eventBaseType = assembly.GetType("UnityEngine.UIElements.EventBase");
            if (clickEventType == null || eventBaseType == null || !eventBaseType.IsAssignableFrom(clickEventType))
                return false;

            var sendEvent = element.GetType().GetMethod(
                "SendEvent",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { eventBaseType },
                null);
            if (sendEvent == null)
                return false;

            object clickEvent = null;
            try
            {
                clickEvent = Activator.CreateInstance(clickEventType, nonPublic: true);
                if (clickEvent == null)
                    return false;

                FocusElementIfPossible(element);
                SetEventProperty(clickEventType, clickEvent, "target", element);
                sendEvent.Invoke(element, new[] { clickEvent });
                invokeMethod = "VisualElement.SendEvent(ClickEvent)";
                return true;
            }
            catch (TargetInvocationException ex)
            {
                error = ex.InnerException ?? ex;
                return false;
            }
            catch (Exception ex)
            {
                error = ex;
                return false;
            }
            finally
            {
                if (clickEvent is IDisposable disposable)
                    disposable.Dispose();
            }
        }

        private static bool TryInvokeClickableMethod(object clickable, string methodName, out string invokeMethod, out Exception error)
        {
            invokeMethod = string.Empty;
            error = null;

            var clickableType = clickable.GetType();
            var eventBaseType = clickableType.Assembly.GetType("UnityEngine.UIElements.EventBase");
            if (eventBaseType == null)
                return false;

            object[] args;
            Type[] paramTypes;
            switch (methodName)
            {
                case "Invoke":
                    args = new object[] { null };
                    paramTypes = new[] { eventBaseType };
                    break;
                case "SimulateSingleClick":
                    args = new object[] { null, 0 };
                    paramTypes = new[] { eventBaseType, typeof(int) };
                    break;
                default:
                    return false;
            }

            var method = clickableType.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                paramTypes,
                null);
            if (method == null)
                return false;

            try
            {
                method.Invoke(clickable, args);
                invokeMethod = $"{clickableType.Name}.{methodName}";
                return true;
            }
            catch (TargetInvocationException ex)
            {
                error = ex.InnerException ?? ex;
                return false;
            }
            catch (Exception ex)
            {
                error = ex;
                return false;
            }
        }

        private static bool GetBoolProp(object obj, string propName, bool defaultValue)
        {
            var prop = obj.GetType().GetProperty(propName);
            if (prop != null && prop.PropertyType == typeof(bool))
                return (bool)prop.GetValue(obj);
            return defaultValue;
        }

        private static void FocusElementIfPossible(object element)
        {
            var focusMethod = element.GetType().GetMethod("Focus", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
            focusMethod?.Invoke(element, Array.Empty<object>());
        }

        private static void SetEventProperty(Type eventType, object eventInstance, string propertyName, object value)
        {
            var property = eventType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            property?.SetValue(eventInstance, value);
        }

        private static string GetElementName(object element)
        {
            var prop = element.GetType().GetProperty("name");
            return prop?.GetValue(element) as string ?? string.Empty;
        }
    }
}
#endif
