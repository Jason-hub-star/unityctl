namespace Unityctl.Core.Diagnostics;

/// <summary>
/// Reads Unity Editor.log to extract recent compilation errors and unityctl status lines.
/// Useful for diagnosing IPC failures caused by plugin compile errors.
/// </summary>
public static class EditorLogDiagnostics
{
    private const int DefaultTailLines = 200;
    private const int MaxCompileErrors = 5;
    private const int MaxAsmdefErrors = 3;
    private const int MaxUnityctlLines = 3;

    /// <summary>
    /// Returns the default Editor.log path for the current platform.
    /// </summary>
    public static string? GetEditorLogPath()
    {
        if (OperatingSystem.IsWindows())
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Unity", "Editor", "Editor.log");
        }

        if (OperatingSystem.IsMacOS())
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library", "Logs", "Unity", "Editor.log");
        }

        // Linux
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config", "unity3d", "Editor.log");
    }

    /// <summary>
    /// Read last <paramref name="tailLines"/> lines from Editor.log and extract CS errors + unityctl status.
    /// Returns null if no diagnostics found or the log is unreadable.
    /// </summary>
    public static string? GetRecentDiagnostics(int tailLines = DefaultTailLines)
        => GetRecentDiagnostics(GetEditorLogPath(), tailLines);

    /// <summary>
    /// Read last <paramref name="tailLines"/> lines from the specified log file and extract CS errors + unityctl status.
    /// Returns null if no diagnostics found or the log is unreadable.
    /// </summary>
    internal static string? GetRecentDiagnostics(string? logPath, int tailLines = DefaultTailLines)
    {
        if (logPath == null || !File.Exists(logPath))
            return null;

        try
        {
            var tail = ReadTail(logPath, tailLines);
            if (tail.Count == 0)
                return null;

            var compileErrors = tail.Where(l => l.Contains("error CS")).Take(MaxCompileErrors).ToList();
            var asmdefErrors = tail
                .Where(l => l.Contains("Required property") && l.Contains(".asmdef"))
                .Take(MaxAsmdefErrors)
                .ToList();
            var unityctlLines = tail.Where(l => l.Contains("[unityctl]")).TakeLast(MaxUnityctlLines).ToList();

            if (compileErrors.Count == 0 && asmdefErrors.Count == 0)
                return null;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("  Unity Editor.log diagnostics:");

            foreach (var e in asmdefErrors)
                sb.AppendLine($"    -> {e.Trim()}");
            foreach (var e in compileErrors)
                sb.AppendLine($"    -> {e.Trim()}");
            foreach (var u in unityctlLines)
                sb.AppendLine($"    {u.Trim()}");

            sb.AppendLine($"  Log: {logPath}");
            return sb.ToString();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Returns structured diagnostics for JSON output: compile errors and unityctl lines as separate lists.
    /// Returns null if no diagnostics found.
    /// </summary>
    public static (List<string> Errors, List<string> UnityctlLines)? GetStructuredDiagnostics(int tailLines = DefaultTailLines)
        => GetStructuredDiagnostics(GetEditorLogPath(), tailLines);

    /// <inheritdoc cref="GetStructuredDiagnostics(int)"/>
    internal static (List<string> Errors, List<string> UnityctlLines)? GetStructuredDiagnostics(string? logPath, int tailLines = DefaultTailLines)
    {
        if (logPath == null || !File.Exists(logPath))
            return null;

        try
        {
            var tail = ReadTail(logPath, tailLines);
            if (tail.Count == 0)
                return null;

            var compileErrors = tail.Where(l => l.Contains("error CS")).Take(MaxCompileErrors).Select(l => l.Trim()).ToList();
            var asmdefErrors = tail
                .Where(l => l.Contains("Required property") && l.Contains(".asmdef"))
                .Take(MaxAsmdefErrors)
                .Select(l => l.Trim())
                .ToList();
            var unityctlLines = tail.Where(l => l.Contains("[unityctl]")).TakeLast(MaxUnityctlLines).Select(l => l.Trim()).ToList();

            var allErrors = new List<string>();
            allErrors.AddRange(asmdefErrors);
            allErrors.AddRange(compileErrors);

            if (allErrors.Count == 0 && unityctlLines.Count == 0)
                return null;

            return (allErrors, unityctlLines);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Read the last N lines from a file using FileShare.ReadWrite to avoid conflicts with Unity.
    /// </summary>
    private static List<string> ReadTail(string path, int lines)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(fs);

        var allLines = new List<string>();
        string? line;
        while ((line = reader.ReadLine()) != null)
            allLines.Add(line);

        return allLines.Skip(Math.Max(0, allLines.Count - lines)).ToList();
    }
}
