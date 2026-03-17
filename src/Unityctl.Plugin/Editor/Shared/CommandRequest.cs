using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Unityctl.Plugin.Editor.Shared
{
    public sealed class CommandRequest
    {
        [JsonProperty("command")]
        public string command = string.Empty;

        [JsonProperty("parameters")]
        public JObject parameters;

        [JsonProperty("requestId")]
        public string requestId = Guid.NewGuid().ToString("N");

        /// <summary>
        /// Get a string parameter. Returns defaultValue if key is missing or null.
        /// </summary>
        public string GetParam(string key, string defaultValue = null)
        {
            if (parameters == null) return defaultValue;
            var token = parameters[key];
            if (token == null || token.Type == JTokenType.Null) return defaultValue;
            return token.Value<string>() ?? defaultValue;
        }

        /// <summary>
        /// Get a typed parameter. Supports int, bool, long, double, float.
        /// Returns defaultValue if key is missing, null, or wrong type.
        /// </summary>
        public T GetParam<T>(string key, T defaultValue = default) where T : struct
        {
            if (parameters == null) return defaultValue;
            var token = parameters[key];
            if (token == null || token.Type == JTokenType.Null) return defaultValue;
            try
            {
                return token.Value<T>();
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Get a nested JObject parameter.
        /// Returns null if key is missing or not an object.
        /// </summary>
        public JObject GetObjectParam(string key)
        {
            if (parameters == null) return null;
            var token = parameters[key];
            return token as JObject;
        }
    }
}
