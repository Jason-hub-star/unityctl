using System.Text.Json;
using Unityctl.Core.FlightRecorder;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Core.Tests.FlightRecorder;

/// <summary>
/// Edge-case and robustness tests for FlightLog.
/// Verifies that malformed NDJSON, null fields, and other anomalies
/// are handled gracefully without throwing exceptions.
/// </summary>
public sealed class FlightLogRobustnessTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FlightLog _log;

    public FlightLogRobustnessTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"unityctl-robust-test-{Guid.NewGuid():N}");
        _log = new FlightLog(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    // ─── Malformed NDJSON ─────────────────────────────────────────────────────

    [Fact]
    public void Query_MalformedJsonLine_IsSkipped()
    {
        // Write a file with mixed good and bad lines
        Directory.CreateDirectory(_tempDir);
        var today = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");
        var filePath = Path.Combine(_tempDir, $"flight-{today}.ndjson");

        var goodEntry = new FlightEntry
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Operation = "build",
            Level = "info",
            V = "0.2.0"
        };
        var goodLine = JsonSerializer.Serialize(goodEntry);

        File.WriteAllLines(filePath, [
            goodLine,
            "not-valid-json{{{",
            goodLine
        ]);

        // Should not throw, and should return the 2 valid entries
        var results = _log.Query(new FlightQuery { Last = 10 });

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void Query_EmptyLines_AreSkipped()
    {
        Directory.CreateDirectory(_tempDir);
        var today = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");
        var filePath = Path.Combine(_tempDir, $"flight-{today}.ndjson");

        var entry = new FlightEntry
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Operation = "ping",
            Level = "info",
            V = "0.2.0"
        };
        var line = JsonSerializer.Serialize(entry);

        File.WriteAllLines(filePath, [
            "",
            "   ",
            line,
            ""
        ]);

        var results = _log.Query(new FlightQuery { Last = 10 });

        Assert.Single(results);
    }

    [Fact]
    public void Query_NullOperationInEntry_DoesNotThrow()
    {
        // Write raw JSON with null "op" — can happen with malformed log entries
        Directory.CreateDirectory(_tempDir);
        var today = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");
        var filePath = Path.Combine(_tempDir, $"flight-{today}.ndjson");
        var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        File.WriteAllText(filePath, $"{{\"ts\":{ts},\"op\":null,\"level\":\"info\",\"v\":\"0.2.0\"}}\n");

        // Query with Op filter — must not throw NullReferenceException
        var results = _log.Query(new FlightQuery { Op = "build", Last = 10 });

        Assert.Empty(results); // null op doesn't match "build"
    }

    [Fact]
    public void Query_NullLevelInEntry_DoesNotThrow()
    {
        // Write raw JSON with null "level" — can happen with malformed log entries
        Directory.CreateDirectory(_tempDir);
        var today = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");
        var filePath = Path.Combine(_tempDir, $"flight-{today}.ndjson");
        var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        File.WriteAllText(filePath, $"{{\"ts\":{ts},\"op\":\"build\",\"level\":null,\"v\":\"0.2.0\"}}\n");

        // Query with Level filter — must not throw NullReferenceException
        var results = _log.Query(new FlightQuery { Level = "error", Last = 10 });

        Assert.Empty(results); // null level doesn't match "error"
    }

    [Fact]
    public void Query_NonNdjsonFile_IsIgnored()
    {
        // Files not matching "flight-*.ndjson" pattern should not be read
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(Path.Combine(_tempDir, "other.txt"), "some-data");
        File.WriteAllText(Path.Combine(_tempDir, "debug.log"), "debug info");

        var results = _log.Query(new FlightQuery { Last = 10 });

        Assert.Empty(results);
    }

    // ─── GetStats edge cases ──────────────────────────────────────────────────

    [Fact]
    public void GetStats_WithMalformedLines_StillCounts()
    {
        Directory.CreateDirectory(_tempDir);
        var today = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");
        var filePath = Path.Combine(_tempDir, $"flight-{today}.ndjson");

        var entry = new FlightEntry
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Operation = "build",
            Level = "info",
            V = "0.2.0"
        };
        var goodLine = JsonSerializer.Serialize(entry);

        File.WriteAllLines(filePath, [goodLine, "bad-json", goodLine]);

        var stats = _log.GetStats();

        // EntryCount counts non-empty lines (includes bad ones)
        Assert.Equal(1, stats.FileCount);
        Assert.True(stats.EntryCount >= 2);
    }

    // ─── Prune edge cases ─────────────────────────────────────────────────────

    [Fact]
    public void Prune_WithNonNdjsonFiles_LeavesThemAlone()
    {
        Directory.CreateDirectory(_tempDir);
        var otherFile = Path.Combine(_tempDir, "notes.txt");
        File.WriteAllText(otherFile, "important notes");

        _log.Prune();

        // Non-flight files should not be deleted
        Assert.True(File.Exists(otherFile));
    }

    [Fact]
    public void Prune_NeverThrows_WhenDirectoryMissing()
    {
        // Log dir does not exist — should not throw
        var missingLog = new FlightLog(Path.Combine(Path.GetTempPath(), $"nonexistent-{Guid.NewGuid():N}"));
        var result = missingLog.Prune();

        Assert.Equal(0, result.DeletedFiles);
        Assert.Equal(0, result.FreedBytes);
    }

    // ─── FilterByProjectPath ──────────────────────────────────────────────────

    [Fact]
    public void Query_FilterByProjectPath_MatchesExact()
    {
        _log.Record(new FlightEntry
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Operation = "build",
            Level = "info",
            Project = "/Projects/MyGame",
            V = "0.2.0"
        });
        _log.Record(new FlightEntry
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Operation = "build",
            Level = "info",
            Project = "/Projects/OtherGame",
            V = "0.2.0"
        });

        var results = _log.Query(new FlightQuery { ProjectPath = "/Projects/MyGame", Last = 10 });

        Assert.Single(results);
        Assert.Equal("/Projects/MyGame", results[0].Project);
    }

    [Fact]
    public void Query_FilterByUntil_ExcludesNewerEntries()
    {
        var todayNoonUtc = new DateTimeOffset(DateTime.UtcNow.Date.AddHours(12), TimeSpan.Zero);
        _log.Record(new FlightEntry
        {
            Timestamp = todayNoonUtc.AddHours(-3).ToUnixTimeMilliseconds(),
            Operation = "old",
            Level = "info",
            V = "0.2.0"
        });
        _log.Record(new FlightEntry
        {
            Timestamp = todayNoonUtc.ToUnixTimeMilliseconds(),
            Operation = "new",
            Level = "info",
            V = "0.2.0"
        });

        var results = _log.Query(new FlightQuery
        {
            Until = todayNoonUtc.AddHours(-1),
            Last = 10
        });

        Assert.Single(results);
        Assert.Equal("old", results[0].Operation);
    }
}
