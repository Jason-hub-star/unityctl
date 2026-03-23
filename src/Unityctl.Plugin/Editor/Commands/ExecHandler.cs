#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    /// <summary>
    /// Handles the "exec" command: evaluates a C# expression in the Unity Editor
    /// using reflection. Supports property get/set and method calls on any type
    /// loaded in the current AppDomain.
    ///
    /// Security: dangerous types (System.IO.File, System.Diagnostics.Process,
    /// System.Net.*, System.Reflection.Emit) are blocked via BlockedTypePatterns.
    /// All other types — including project code — are allowed.
    /// This handler assumes a trusted agent caller.
    /// </summary>
    public class ExecHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.Exec;

        // Type name patterns that are always blocked for safety
        private static readonly string[] BlockedTypePatterns =
        {
            "System.IO.File",
            "System.IO.Directory",
            "System.IO.Path",
            "System.Diagnostics.Process",
            "System.Net.",
            "System.Reflection.Emit",
            "System.Runtime.InteropServices.Marshal"
        };

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var code = request.GetParam("code");
            if (string.IsNullOrWhiteSpace(code))
                return InvalidParameters("'code' parameter is required.");

            try
            {
                var result = EvaluateExpression(code.Trim());
                var data = new JObject { ["result"] = result != null ? JToken.FromObject(result) : JValue.CreateNull() };
                return Ok($"exec: {code}", data);
            }
            catch (ExecSecurityException ex)
            {
                return Fail(StatusCode.InvalidParameters, $"Security violation: {ex.Message}");
            }
            catch (ExecParseException ex)
            {
                return Fail(StatusCode.InvalidParameters, $"Parse error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return Fail(StatusCode.UnknownError, $"Execution error: {ex.Message}");
            }
        }

        /// <summary>
        /// Evaluates a simple C# expression via reflection.
        /// Supported patterns:
        ///   TypeName.PropertyName                   (get static property)
        ///   TypeName.PropertyName = value           (set static property)
        ///   TypeName.MethodName()                   (call static method, no args)
        ///   TypeName.MethodName(arg1, arg2, ...)    (call static method with args)
        /// </summary>
        private object? EvaluateExpression(string expr)
        {
            // Assignment: Type.Member = value
            var eqIdx = expr.IndexOf('=');
            if (eqIdx > 0 && !expr.Contains("=="))
            {
                var lhs = expr.Substring(0, eqIdx).Trim();
                var rhs = expr.Substring(eqIdx + 1).Trim();
                return SetMember(lhs, rhs);
            }

            // Method call or property get
            if (expr.Contains('('))
                return InvokeMethod(expr);

            return GetMember(expr);
        }

        private object? GetMember(string expr)
        {
            var (type, memberName) = ResolveTypeMember(expr);
            var prop = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.Static);
            if (prop != null) return prop.GetValue(null);

            var field = type.GetField(memberName, BindingFlags.Public | BindingFlags.Static);
            if (field != null) return field.GetValue(null);

            throw new ExecParseException($"No public static property or field '{memberName}' on {type.FullName}.");
        }

        private object? SetMember(string lhs, string rhs)
        {
            var (type, memberName) = ResolveTypeMember(lhs);
            var prop = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.Static);
            if (prop != null)
            {
                var value = ConvertValue(rhs, prop.PropertyType);
                prop.SetValue(null, value);
                return value;
            }

            var field = type.GetField(memberName, BindingFlags.Public | BindingFlags.Static);
            if (field != null)
            {
                var value = ConvertValue(rhs, field.FieldType);
                field.SetValue(null, value);
                return value;
            }

            throw new ExecParseException($"No settable public static property or field '{memberName}' on {type.FullName}.");
        }

        private object? InvokeMethod(string expr)
        {
            var parenIdx = expr.IndexOf('(');
            var methodPath = expr.Substring(0, parenIdx).Trim();
            var argsStr = expr.Substring(parenIdx + 1).TrimEnd(')').Trim();

            var (type, methodName) = ResolveTypeMember(methodPath);

            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == methodName)
                .ToArray();

            if (methods.Length == 0)
                throw new ExecParseException($"No public static method '{methodName}' on {type.FullName}.");

            // Pick method based on arg count
            var args = string.IsNullOrWhiteSpace(argsStr)
                ? Array.Empty<object?>()
                : argsStr.Split(',').Select(a => (object?)a.Trim()).ToArray();

            var method = methods.FirstOrDefault(m => m.GetParameters().Length == args.Length)
                ?? methods[0];

            // Convert args to parameter types
            var parameters = method.GetParameters();
            var convertedArgs = new object?[Math.Min(args.Length, parameters.Length)];
            for (var i = 0; i < convertedArgs.Length; i++)
                convertedArgs[i] = ConvertValue(args[i]?.ToString() ?? string.Empty, parameters[i].ParameterType);

            return method.Invoke(null, convertedArgs);
        }

        private (Type type, string memberName) ResolveTypeMember(string expr)
        {
            var lastDot = expr.LastIndexOf('.');
            if (lastDot < 0)
                throw new ExecParseException($"Expression must be 'TypeName.MemberName', got: {expr}");

            var typePart = expr.Substring(0, lastDot).Trim();
            var memberName = expr.Substring(lastDot + 1).Trim();

            var type = ResolveType(typePart)
                ?? throw new ExecParseException($"Type not found: '{typePart}'. Ensure the type is loaded in the current AppDomain.");

            return (type, memberName);
        }

        private static Type? ResolveType(string typeName)
        {
            // Block dangerous types (file I/O, process exec, network, native interop)
            foreach (var blocked in BlockedTypePatterns)
            {
                if (typeName.StartsWith(blocked, StringComparison.OrdinalIgnoreCase))
                    throw new ExecSecurityException($"Type '{typeName}' is blocked for safety.");
            }

            // Search all loaded assemblies
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = asm.GetType(typeName, throwOnError: false, ignoreCase: false);
                if (type != null) return type;
            }

            return null;
        }

        private static object? ConvertValue(string raw, Type targetType)
        {
            if (targetType == typeof(bool))
            {
                return bool.TryParse(raw, out var b) ? b
                    : raw == "1" ? true
                    : raw == "0" ? (object)false
                    : throw new ExecParseException($"Cannot convert '{raw}' to bool.");
            }
            if (targetType == typeof(int)) return int.Parse(raw);
            if (targetType == typeof(float)) return float.Parse(raw, System.Globalization.CultureInfo.InvariantCulture);
            if (targetType == typeof(double)) return double.Parse(raw, System.Globalization.CultureInfo.InvariantCulture);
            if (targetType == typeof(string)) return raw.Trim('"').Trim('\'');

            return Convert.ChangeType(raw, targetType, System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    internal sealed class ExecSecurityException : Exception
    {
        public ExecSecurityException(string message) : base(message) { }
    }

    internal sealed class ExecParseException : Exception
    {
        public ExecParseException(string message) : base(message) { }
    }
}
#endif
