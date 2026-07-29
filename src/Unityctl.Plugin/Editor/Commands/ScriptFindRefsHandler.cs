using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class ScriptFindRefsHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.ScriptFindRefs;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var symbol = request.GetParam("symbol", null);
            if (string.IsNullOrEmpty(symbol))
                return InvalidParameters("Parameter 'symbol' is required.");

            var folder = request.GetParam("folder", "Assets");
            var limit = request.GetParam<int>("limit");
            if (limit <= 0) limit = 500;

            // Find all MonoScript assets
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:MonoScript",
                new[] { folder });

            var references = new JArray();
            int scannedFiles = 0;

            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    continue;

                var fullPath = System.IO.Path.Combine(
                    UnityEngine.Application.dataPath,
                    "..",
                    path);

                if (!System.IO.File.Exists(fullPath))
                    continue;

                scannedFiles++;

                try
                {
                    var lines = System.IO.File.ReadAllLines(fullPath);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        int searchStart = 0;
                        while (searchStart < lines[i].Length)
                        {
                            int col = FindWordBoundary(lines[i], symbol, searchStart);
                            if (col < 0) break;

                            references.Add(new JObject
                            {
                                ["file"] = path,
                                ["line"] = i + 1,
                                ["column"] = col + 1,
                                ["context"] = lines[i].TrimEnd()
                            });

                            if (references.Count >= limit)
                                break;

                            searchStart = col + symbol.Length;
                        }

                        if (references.Count >= limit)
                            break;
                    }
                }
                catch
                {
                    // Skip unreadable files
                }

                if (references.Count >= limit)
                    break;
            }

            return Ok($"Found {references.Count} reference(s) to '{symbol}'", new JObject
            {
                ["symbol"] = symbol,
                ["references"] = references,
                ["referenceCount"] = references.Count,
                ["scannedFiles"] = scannedFiles,
                ["truncated"] = references.Count >= limit,
                ["note"] = "Text-based search; may include matches in comments/strings"
            });
#else
            return NotInEditor();
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// Finds symbol with word-boundary check (not inside another identifier).
        /// Returns the 0-based column index, or -1 if not found.
        /// </summary>
        private static int FindWordBoundary(string line, string symbol, int startIdx)
        {
            while (true)
            {
                int idx = line.IndexOf(symbol, startIdx, StringComparison.Ordinal);
                if (idx < 0) return -1;

                // Check word boundary before
                bool leftOk = idx == 0 || !IsIdentChar(line[idx - 1]);
                // Check word boundary after
                int afterIdx = idx + symbol.Length;
                bool rightOk = afterIdx >= line.Length || !IsIdentChar(line[afterIdx]);

                if (leftOk && rightOk) return idx;

                startIdx = idx + 1;
            }
        }

        private static bool IsIdentChar(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }
#endif
    }
}
