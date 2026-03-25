using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Unityctl.Plugin.Editor.Utilities
{
#if UNITY_EDITOR
    internal sealed class UnityctlProjectSettingsData
    {
        public bool Enabled { get; set; }
        public string InstallSourceKind { get; set; }
        public string InstalledVersion { get; set; }
    }

    internal static class UnityctlProjectSettingsStore
    {
        private const string SettingsFileName = "UnityctlSettings.asset";

        public static UnityctlProjectSettingsData LoadCurrent()
        {
            var projectPath = Path.GetDirectoryName(Application.dataPath);
            return string.IsNullOrWhiteSpace(projectPath) ? null : Load(projectPath);
        }

        public static UnityctlProjectSettingsData Load(string projectPath)
        {
            var settingsPath = GetSettingsPath(projectPath);
            if (!File.Exists(settingsPath))
                return null;

            try
            {
                return JsonConvert.DeserializeObject<UnityctlProjectSettingsData>(File.ReadAllText(settingsPath));
            }
            catch
            {
                return null;
            }
        }

        public static string GetSettingsPath(string projectPath)
        {
            return Path.Combine(projectPath, "ProjectSettings", SettingsFileName);
        }
    }
#endif
}
