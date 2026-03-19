using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class DoctorCommandTests
{
    [CliTestFact]
    public void ShouldAutoDiagnose_ProjectLocked_ReturnsTrue()
    {
        var response = new CommandResponse
        {
            StatusCode = StatusCode.ProjectLocked,
            Success = false,
            Message = "locked"
        };

        Assert.True(DoctorCommand.ShouldAutoDiagnose(response));
    }

    [CliTestFact]
    public void ShouldAutoDiagnose_CommandNotFound_ReturnsTrue()
    {
        var response = new CommandResponse
        {
            StatusCode = StatusCode.CommandNotFound,
            Success = false,
            Message = "Unknown command: gameobject-find"
        };

        Assert.True(DoctorCommand.ShouldAutoDiagnose(response));
    }

    [CliTestFact]
    public void ShouldAutoDiagnose_UnknownErrorWithPipeMessage_ReturnsTrue()
    {
        var response = new CommandResponse
        {
            StatusCode = StatusCode.UnknownError,
            Success = false,
            Message = "IPC communication error: Pipe closed before full message was read."
        };

        Assert.True(DoctorCommand.ShouldAutoDiagnose(response));
    }

    [CliTestFact]
    public void ShouldAutoDiagnose_NotFound_ReturnsFalse()
    {
        var response = new CommandResponse
        {
            StatusCode = StatusCode.NotFound,
            Success = false,
            Message = "Asset not found"
        };

        Assert.False(DoctorCommand.ShouldAutoDiagnose(response));
    }

    [CliTestFact]
    public void ShouldAutoDiagnose_Success_ReturnsFalse()
    {
        var response = CommandResponse.Ok("ok");
        Assert.False(DoctorCommand.ShouldAutoDiagnose(response));
    }

    [CliTestFact]
    public void Diagnose_IncludesBuildStateDirectory()
    {
        var tempProject = Path.Combine(Path.GetTempPath(), "unityctl-doctor-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(tempProject, "Packages"));
        File.WriteAllText(Path.Combine(tempProject, "Packages", "manifest.json"), "{ }");

        try
        {
            var result = DoctorCommand.Diagnose(tempProject);

            Assert.False(string.IsNullOrWhiteSpace(result.BuildStateDirectory));
            Assert.True(Path.IsPathRooted(result.BuildStateDirectory));
        }
        finally
        {
            try { Directory.Delete(tempProject, recursive: true); } catch { }
        }
    }
}
