using System.Text.Json.Nodes;
using Unityctl.Shared;
using Unityctl.Shared.Protocol;

namespace Unityctl.Core.CodeIntelligence;

internal static class ScriptReferenceScanner
{
    internal static CommandResponse Execute(
        string projectPath,
        CommandRequest request,
        CancellationToken ct = default)
    {
        var symbol = request.GetParam("symbol");
        if (string.IsNullOrWhiteSpace(symbol))
            return Fail(request, StatusCode.InvalidParameters, "Parameter 'symbol' is required.");

        var projectRoot = Path.GetFullPath(projectPath);
        var folder = request.GetParam("folder", "Assets")!;
        var searchRoot = Path.GetFullPath(Path.Combine(projectRoot, folder));
        var relativeRoot = Path.GetRelativePath(projectRoot, searchRoot);
        if (relativeRoot == ".."
            || relativeRoot.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || relativeRoot.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal))
        {
            return Fail(request, StatusCode.InvalidParameters, "Search folder must be inside the Unity project.");
        }

        if (!Directory.Exists(searchRoot))
            return Fail(request, StatusCode.NotFound, $"Search folder not found: {folder}");

        var limit = request.GetParam("limit", 500);
        if (limit <= 0) limit = 500;

        string[] files;
        try
        {
            files = Directory.EnumerateFiles(searchRoot, "*.cs", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Fail(request, StatusCode.UnknownError, $"Could not enumerate scripts: {ex.Message}");
        }

        var references = new JsonArray();
        var scannedFiles = 0;
        var truncated = false;

        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            scannedFiles++;

            try
            {
                var lineNumber = 0;
                foreach (var line in File.ReadLines(file))
                {
                    ct.ThrowIfCancellationRequested();
                    lineNumber++;
                    var searchStart = 0;
                    while (searchStart < line.Length)
                    {
                        var column = FindWordBoundary(line, symbol, searchStart);
                        if (column < 0) break;

                        if (references.Count >= limit)
                        {
                            truncated = true;
                            goto Complete;
                        }

                        references.Add(new JsonObject
                        {
                            ["file"] = NormalizePath(Path.GetRelativePath(projectRoot, file)),
                            ["line"] = lineNumber,
                            ["column"] = column + 1,
                            ["context"] = line.TrimEnd()
                        });
                        searchStart = column + symbol.Length;
                    }
                }
            }
            catch (IOException)
            {
                // Match the Editor bridge behavior: unreadable scripts do not fail the search.
            }
            catch (UnauthorizedAccessException)
            {
                // Match the Editor bridge behavior: unreadable scripts do not fail the search.
            }
        }

    Complete:
        var response = CommandResponse.Ok(
            $"Found {references.Count} reference(s) to '{symbol}'",
            new JsonObject
            {
                ["symbol"] = symbol,
                ["references"] = references,
                ["referenceCount"] = references.Count,
                ["scannedFiles"] = scannedFiles,
                ["truncated"] = truncated,
                ["note"] = "Text-based local search; may include matches in comments/strings",
                ["target"] = new JsonObject
                {
                    ["projectPath"] = Constants.NormalizeProjectPath(projectRoot),
                    ["pipeName"] = Constants.GetPipeName(projectRoot),
                    ["transport"] = "local",
                    ["requiresEditor"] = false
                }
            });
        response.RequestId = request.RequestId;
        return response;
    }

    private static CommandResponse Fail(CommandRequest request, StatusCode code, string message)
    {
        var response = CommandResponse.Fail(code, message);
        response.RequestId = request.RequestId;
        return response;
    }

    private static int FindWordBoundary(string line, string symbol, int startIndex)
    {
        while (true)
        {
            var index = line.IndexOf(symbol, startIndex, StringComparison.Ordinal);
            if (index < 0) return -1;

            var leftOk = index == 0 || !IsIdentifierCharacter(line[index - 1]);
            var afterIndex = index + symbol.Length;
            var rightOk = afterIndex >= line.Length || !IsIdentifierCharacter(line[afterIndex]);
            if (leftOk && rightOk) return index;

            startIndex = index + 1;
        }
    }

    private static bool IsIdentifierCharacter(char value) =>
        char.IsLetterOrDigit(value) || value == '_';

    private static string NormalizePath(string path) =>
        path.Replace('\\', '/');
}
