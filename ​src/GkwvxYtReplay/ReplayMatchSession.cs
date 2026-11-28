using System;
using System.Collections.Generic;

namespace GkwvxYtReplay;

public sealed class ReplayMatchSession
{
    private readonly object _gate = new();
    private readonly string _replaysDirectory;
    private readonly int _maxEvents;
    private readonly int _flushInterval;

    private NdjsonMatchRecorder? _recorder;
    private string? _currentMatchId;

    public bool IsRecording
    {
        get
        {
            lock (_gate)
            {
                return _recorder is not null && !_recorder.ReachedEventLimit;
            }
        }
    }

    public ReplayMatchSession(string replaysDirectory, int maxEvents = 50000, int flushInterval = 50)
    {
        if (string.IsNullOrWhiteSpace(replaysDirectory))
        {
            throw new ArgumentException("A pasta de replays não pode ser vazia.", nameof(replaysDirectory));
        }

        _replaysDirectory = replaysDirectory;
        _maxEvents = Math.Max(1, maxEvents);
        _flushInterval = Math.Max(1, flushInterval);
    }

    public void StartMatch(string matchId, Dictionary<string, string>? initialMetadata = null)
    {
        lock (_gate)
        {
            StopInternalLocked();

            string safeMatchId = string.IsNullOrWhiteSpace(matchId) ? "match" : matchId.Trim();
            _currentMatchId = safeMatchId;
            _recorder = new NdjsonMatchRecorder(_replaysDirectory, safeMatchId, _maxEvents, _flushInterval);
            _recorder.TryRecord("match_start", "harmony", $"Partida iniciada: {safeMatchId}", initialMetadata);
        }
    }

    public bool RecordEvent(string eventType, string actor, string summary, Dictionary<string, string>? metadata = null)
    {
        lock (_gate)
        {
            if (_recorder is null)
            {
                return false;
            }

            return _recorder.TryRecord(eventType, actor, summary, metadata);
        }
    }

    public ReplayMatchSummary? StopMatch(string reason = "match_end")
    {
        lock (_gate)
        {
            return StopInternalLocked(reason);
        }
    }

    private ReplayMatchSummary? StopInternalLocked(string reason = "match_end")
    {
        if (_recorder is null)
        {
            return null;
        }

        try
        {
            _recorder.TryRecord("match_end", "harmony", $"Partida encerrada ({reason})", new Dictionary<string, string>
            {
                ["match_id"] = _currentMatchId ?? "unknown",
                ["reason"] = reason
            });

            return _recorder.BuildSummary();
        }
        finally
        {
            _recorder.Dispose();
            _recorder = null;
            _currentMatchId = null;
        }
    }
}
