namespace Unityctl.Shared;

public static class Constants
{
    public const string Version = "0.1.0";
    public const string PipePrefix = "unityctl_";
    public const int DefaultTimeoutMs = 120_000;
    public const int PingTimeoutMs = 10_000;
    public const int BatchModeTimeoutMs = 600_000;
    public const string PluginPackageName = "com.unityctl.bridge";
    public const string BatchEntryMethod = "Unityctl.Plugin.Editor.BatchMode.UnityctlBatchEntry.Execute";
}
