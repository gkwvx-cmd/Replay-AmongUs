using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace GkwvxYtReplay;

public sealed record ReplayEventEntry(
    long TimestampMs,
    string EventType,
    string Actor,
    string Summary,
    Dictionary<string, string>? Metadata = null
);

public sealed record ReplayMatchSummary(
    string FilePath,
    int EventCount,
    long FirstTimestampMs,
    long LastTimestampMs,
    bool ReachedEventLimit
);

public sealed class NdjsonMatchRecorder : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    private readonly string _filePath;
    private readonly int _maxEvents;
    private readonly int _flushInterval;
    private readonly StreamWriter _writer;
    private readonly object _syncLock = new();

    private int _writtenCount;
    private long _firstTimestampMs;
    private long _lastTimestampMs;
    private bool _disposed;

    public string FilePath => _filePath;
    public int WrittenCount => _writtenCount;
    public bool ReachedEventLimit => _writtenCount >= _maxEvents;

    public NdjsonMatchRecorder(string replaysDirectory, string matchId, int maxEvents = 50000, int flushInterval = 50)
    {
        if (string.IsNullOrWhiteSpace(replaysDirectory))
        {
            throw new ArgumentException("A pasta de replays não pode ser vazia.", nameof(replaysDirectory));
        }

        if (string.IsNullOrWhiteSpace(matchId))
        {
            throw new ArgumentException("O identificador da partida não pode ser vazio.", nameof(matchId));
        }

        _maxEvents = Math.Max(1, maxEvents);
        _flushInterval = Math.Max(1, flushInterval);

        if (!Directory.Exists(replaysDirectory))
        {
            Directory.CreateDirectory(replaysDirectory);
        }

        string safeId = SanitizeFileName(matchId);
        string fileName = $"{safeId}_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.ndjson";
        _filePath = Path.Combine(replaysDirectory, fileName);

        var stream = new FileStream(_filePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        _writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
        {
            AutoFlush = false
        };
    }

    public bool TryRecord(string eventType, string actor, string summary, Dictionary<string, string>? metadata = null)
    {
        lock (_syncLock)
        {
            if (_disposed || _writtenCount >= _maxEvents)
            {
                return false;
            }

            long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (_writtenCount == 0)
            {
                _firstTimestampMs = nowMs;
            }
            _lastTimestampMs = nowMs;

            var entry = new ReplayEventEntry(
                TimestampMs: nowMs,
                EventType: string.IsNullOrWhiteSpace(eventType) ? "unknown" : eventType.Trim(),
                Actor: string.IsNullOrWhiteSpace(actor) ? "system" : actor.Trim(),
                Summary: summary ?? string.Empty,
                Metadata: metadata
            );

            string jsonLine = JsonSerializer.Serialize(entry, JsonOptions);
            _writer.WriteLine(jsonLine);
            _writtenCount++;

            if (_writtenCount % _flushInterval == 0)
            {
                _writer.Flush();
            }

            return true;
        }
    }

    public ReplayMatchSummary BuildSummary()
    {
        lock (_syncLock)
        {
            _writer.Flush();
            return new ReplayMatchSummary(
                FilePath: _filePath,
                EventCount: _writtenCount,
                FirstTimestampMs: _firstTimestampMs,
                LastTimestampMs: _lastTimestampMs,
                ReachedEventLimit: _writtenCount >= _maxEvents
            );
        }
    }

    public void Dispose()
    {
        lock (_syncLock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            try
            {
                _writer.Flush();
            }
            finally
            {
                _writer.Dispose();
            }
        }
    }

    private static string SanitizeFileName(string raw)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            raw = raw.Replace(invalid, '_');
        }
        return raw.Trim();
    }
}
