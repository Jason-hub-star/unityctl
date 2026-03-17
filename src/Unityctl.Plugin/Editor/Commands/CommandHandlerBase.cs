using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public abstract class CommandHandlerBase : IUnityctlCommand
    {
        public abstract string CommandName { get; }

        public CommandResponse Execute(CommandRequest request)
        {
#if UNITY_EDITOR
            try
            {
                return ExecuteInEditor(request);
            }
            catch (Exception e)
            {
                return HandleException(e);
            }
#else
            return NotInEditor();
#endif
        }

        protected abstract CommandResponse ExecuteInEditor(CommandRequest request);

        protected virtual CommandResponse HandleException(Exception exception)
        {
            return Fail(
                StatusCode.UnknownError,
                exception.Message,
                errors: GetStackTrace(exception));
        }

        protected static CommandResponse Ok(string message = null, JObject data = null)
        {
            return CommandResponse.Ok(message, data);
        }

        protected static CommandResponse Ok(StatusCode code, string message, JObject data = null)
        {
            return Response(code, true, message, data);
        }

        protected static CommandResponse Fail(
            StatusCode code,
            string message,
            JObject data = null,
            List<string> errors = null)
        {
            return Response(code, false, message, data, errors);
        }

        protected static CommandResponse InvalidParameters(string message)
        {
            return Fail(StatusCode.InvalidParameters, message);
        }

        protected static CommandResponse NotInEditor()
        {
            return Fail(StatusCode.UnknownError, "Not running in Unity Editor");
        }

        protected static List<string> GetStackTrace(Exception exception)
        {
            return exception.StackTrace == null
                ? null
                : new List<string> { exception.StackTrace };
        }

        private static CommandResponse Response(
            StatusCode code,
            bool success,
            string message,
            JObject data = null,
            List<string> errors = null)
        {
            return new CommandResponse
            {
                statusCode = (int)code,
                success = success,
                message = message,
                data = data,
                errors = errors
            };
        }
    }
}
