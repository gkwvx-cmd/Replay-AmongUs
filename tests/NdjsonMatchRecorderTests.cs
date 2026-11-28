using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace GkwvxYtReplay.Tests;

public sealed class NdjsonMatchRecorderTests
{
    private static string CreateTempDir()
    {
        string path = Path.Combine(Path.GetTempPath(), "gkwvxyt_recorder_tests_" + Guid.NewGuid().ToString("N"));
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
    public void TryRecord_WritesValidNdjsonLinesAndBuildsSummary()
    {
        string tempDir = CreateTempDir();
        try
        {
            var meta = new Dictionary<string, string>
            {
                ["map"] = "Skeld",
                ["phase"] = "Gameplay"
            };

            string filePath;
            ReplayMatchSummary summary;

            using (var recorder = new NdjsonMatchRecorder(tempDir, "match_01", maxEvents: 100, flushInterval: 1))
            {
                Assert.True(recorder.TryRecord("match_start", "host", "Partida iniciada", meta));
                Assert.True(recorder.TryRecord("kill", "impostor", "Eliminação registrada", null));

                filePath = recorder.FilePath;
                summary = recorder.BuildSummary();

                Assert.Equal(2, recorder.WrittenCount);
                Assert.Equal(2, summary.EventCount);
                Assert.False(summary.ReachedEventLimit);
                Assert.True(summary.FirstTimestampMs > 0);
                Assert.True(summary.LastTimestampMs >= summary.FirstTimestampMs);
            }

            Assert.True(File.Exists(filePath));
            string[] lines = File.ReadAllLines(filePath);
            Assert.Equal(2, lines.Length);

            using JsonDocument doc0 = JsonDocument.Parse(lines[0]);
            Assert.Equal("match_start", doc0.RootElement.GetProperty("EventType").GetString());
            Assert.Equal("host", doc0.RootElement.GetProperty("Actor").GetString());
            Assert.Equal("Skeld", doc0.RootElement.GetProperty("Metadata").GetProperty("map").GetString());

            using JsonDocument doc1 = JsonDocument.Parse(lines[1]);
            Assert.Equal("kill", doc1.RootElement.GetProperty("EventType").GetString());
        }
        finally
        {
            CleanupDir(tempDir);
        }
    }

    [Fact]
    public void TryRecord_RespectsMaxEventsLimit()
    {
        string tempDir = CreateTempDir();
        try
        {
            using var recorder = new NdjsonMatchRecorder(tempDir, "match_limit", maxEvents: 2, flushInterval: 1);

            Assert.True(recorder.TryRecord("ev1", "sys", "Um", null));
            Assert.True(recorder.TryRecord("ev2", "sys", "Dois", null));
            Assert.False(recorder.TryRecord("ev3", "sys", "Excedente", null));

            Assert.True(recorder.ReachedEventLimit);
            Assert.Equal(2, recorder.WrittenCount);

            ReplayMatchSummary summary = recorder.BuildSummary();
            Assert.Equal(2, summary.EventCount);
            Assert.True(summary.ReachedEventLimit);
        }
        finally
        {
            CleanupDir(tempDir);
        }
    }

    [Fact]
    public void Constructor_ThrowsOnInvalidArguments()
    {
        Assert.Throws<ArgumentException>(() => new NdjsonMatchRecorder("", "match_01"));
        Assert.Throws<ArgumentException>(() => new NdjsonMatchRecorder("   ", "match_01"));
        Assert.Throws<ArgumentException>(() => new NdjsonMatchRecorder("/tmp/replays", ""));
    }
}
