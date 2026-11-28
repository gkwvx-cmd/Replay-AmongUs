using System;
using System.IO;
using System.Linq;
using Xunit;

namespace GkwvxYtReplay.Tests;

public sealed class NdjsonReplayReaderTests
{
    private static string CreateTempDir()
    {
        string path = Path.Combine(Path.GetTempPath(), "gkwvxyt_reader_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void CleanupDir(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    [Fact]
    public void Summarize_ReadsRecordedNdjsonAndCountsEventTypes()
    {
        string tempDir = CreateTempDir();
        try
        {
            string filePath;
            using (var recorder = new NdjsonMatchRecorder(tempDir, "summary_match", maxEvents: 50, flushInterval: 1))
            {
                recorder.TryRecord("match_start", "host", "Início");
                recorder.TryRecord("player_murder", "impostor", "Kill 1");
                recorder.TryRecord("player_murder", "impostor", "Kill 2");
                filePath = recorder.FilePath;
            }

            ReplayInspectionSummary summary = NdjsonReplayReader.Summarize(filePath);
            var events = NdjsonReplayReader.ReadEvents(filePath);

            Assert.Equal(3, summary.TotalEvents);
            Assert.Equal(0, summary.MalformedLines);
            Assert.Equal(3, events.Count);
            Assert.Equal(1, summary.EventsByType["match_start"]);
            Assert.Equal(2, summary.EventsByType["player_murder"]);
            Assert.True(summary.DurationMs >= 0);
        }
        finally
        {
            CleanupDir(tempDir);
        }
    }

    [Fact]
    public void Summarize_SkipsMalformedLinesWhenEnabled()
    {
        string tempDir = CreateTempDir();
        try
        {
            string filePath = Path.Combine(tempDir, "corrupted.ndjson");
            File.WriteAllLines(filePath, new[]
            {
                """{"TimestampMs":1000,"EventType":"match_start","Actor":"host","Summary":"ok"}""",
                """{linha_quebrada_json""",
                """{"TimestampMs":2500,"EventType":"match_end","Actor":"host","Summary":"fim"}"""
            });

            ReplayInspectionSummary summary = NdjsonReplayReader.Summarize(filePath, skipMalformedLines: true);

            Assert.Equal(2, summary.TotalEvents);
            Assert.Equal(1, summary.MalformedLines);
            Assert.Equal(1500, summary.DurationMs);
            Assert.Equal(2, NdjsonReplayReader.ReadEvents(filePath, skipMalformedLines: true).Count);
        }
        finally
        {
            CleanupDir(tempDir);
        }
    }
}
