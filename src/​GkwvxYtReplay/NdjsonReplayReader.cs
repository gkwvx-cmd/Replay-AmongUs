using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace GkwvxYtReplay;

public sealed record ReplayInspectionSummary(
    string FilePath,
    int TotalEvents,
    int MalformedLines,
    long? FirstTimestampMs,
    long? LastTimestampMs,
    long DurationMs,
    IReadOnlyDictionary<string, int> EventsByType
);

public static class NdjsonReplayReader
{
    public static IReadOnlyList<ReplayEventEntry> ReadEvents(string ndjsonFilePath, bool skipMalformedLines = true)
    {
        if (string.IsNullOrWhiteSpace(ndjsonFilePath))
        {
            throw new ArgumentException("O caminho do arquivo .ndjson não pode ser vazio.", nameof(ndjsonFilePath));
        }

        if (!File.Exists(ndjsonFilePath))
        {
            throw new FileNotFoundException("Arquivo de replay não encontrado.", ndjsonFilePath);
        }

        var events = new List<ReplayEventEntry>();

        using var stream = new FileStream(ndjsonFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        int lineNumber = 0;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                ReplayEventEntry? entry = JsonSerializer.Deserialize<ReplayEventEntry>(line);
                if (entry is not null)
                {
                    events.Add(entry);
                }
                else if (!skipMalformedLines)
                {
                    throw new InvalidDataException($"Linha {lineNumber} retornou evento nulo.");
                }
            }
            catch (JsonException ex) when (skipMalformedLines)
            {
                _ = ex;
            }
            catch (InvalidDataException) when (skipMalformedLines)
            {
            }
        }

        return events;
    }

    public static ReplayInspectionSummary Summarize(string ndjsonFilePath, bool skipMalformedLines = true)
    {
        if (string.IsNullOrWhiteSpace(ndjsonFilePath))
        {
            throw new ArgumentException("O caminho do arquivo .ndjson não pode ser vazio.", nameof(ndjsonFilePath));
        }

        if (!File.Exists(ndjsonFilePath))
        {
            throw new FileNotFoundException("Arquivo de replay não encontrado.", ndjsonFilePath);
        }

        var countsByType = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        int totalEvents = 0;
        int malformedLines = 0;
        long? firstTimestampMs = null;
        long? lastTimestampMs = null;

        using var stream = new FileStream(ndjsonFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        int lineNumber = 0;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                ReplayEventEntry? entry = JsonSerializer.Deserialize<ReplayEventEntry>(line);
                if (entry is null)
                {
                    malformedLines++;
                    if (!skipMalformedLines)
                    {
                        throw new InvalidDataException($"Linha {lineNumber} vazia ou inválida.");
                    }
                    continue;
                }

                totalEvents++;
                string typeKey = string.IsNullOrWhiteSpace(entry.EventType) ? "unknown" : entry.EventType.Trim();
                countsByType[typeKey] = countsByType.TryGetValue(typeKey, out int current) ? current + 1 : 1;

                if (firstTimestampMs is null || entry.TimestampMs < firstTimestampMs.Value)
                {
                    firstTimestampMs = entry.TimestampMs;
                }

                if (lastTimestampMs is null || entry.TimestampMs > lastTimestampMs.Value)
                {
                    lastTimestampMs = entry.TimestampMs;
                }
            }
            catch (JsonException)
            {
                malformedLines++;
                if (!skipMalformedLines)
                {
                    throw;
                }
            }
            catch (InvalidDataException)
            {
                malformedLines++;
                if (!skipMalformedLines)
                {
                    throw;
                }
            }
        }

        long durationMs = (firstTimestampMs is not null && lastTimestampMs is not null && lastTimestampMs.Value >= firstTimestampMs.Value)
            ? lastTimestampMs.Value - firstTimestampMs.Value
            : 0;

        return new ReplayInspectionSummary(
            FilePath: ndjsonFilePath,
            TotalEvents: totalEvents,
            MalformedLines: malformedLines,
            FirstTimestampMs: firstTimestampMs,
            LastTimestampMs: lastTimestampMs,
            DurationMs: durationMs,
            EventsByType: countsByType
        );
    }
}
