using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Unityctl.Plugin.Editor.Ipc
{
    /// <summary>
    /// Deterministic pipe name generation.
    /// Must produce identical output to Constants.GetPipeName() in Shared.
    /// </summary>
    public static class PipeNameHelper
    {
        public const string PipePrefix = "unityctl_";

        public static string NormalizeProjectPath(string projectPath)
        {
            var full = Path.GetFullPath(projectPath);
#if UNITY_EDITOR_WIN
            full = full.ToLowerInvariant();
#endif
            full = full.Replace('\\', '/');
            full = full.TrimEnd('/');
            return full;
        }

        public static string GetPipeName(string projectPath)
        {
            var normalized = NormalizeProjectPath(projectPath);
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(normalized));
                var sb = new StringBuilder(hash.Length * 2);
                foreach (var b in hash)
                    sb.Append(b.ToString("x2"));
                return PipePrefix + sb.ToString().Substring(0, 16);
            }
        }
    }
}
