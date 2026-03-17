namespace Unityctl.Plugin.Editor.Shared
{
    public enum StatusCode
    {
        // Success (0xx)
        Ready = 0,

        // Transient — retriable (1xx)
        Compiling = 100,
        Reloading = 101,
        EnteringPlayMode = 102,
        Busy = 103,
        Accepted = 104,

        // Fatal — immediate failure (2xx)
        NotFound = 200,
        ProjectLocked = 201,
        LicenseError = 202,
        PluginNotInstalled = 203,

        // Error (5xx)
        UnknownError = 500,
        CommandNotFound = 501,
        InvalidParameters = 502,
        BuildFailed = 503,
        TestFailed = 504
    }
}
