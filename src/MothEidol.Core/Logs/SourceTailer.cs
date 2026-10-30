namespace MothEidol.Core.Logs;

public sealed record PollResult(IReadOnlyList<string> Lines, SourceStatus Status, bool HasMore, string? Error);

/// <summary>
/// Reads the new complete lines of one source on each <see cref="Poll"/>. It carries no timers and no UI:
/// the caller decides when to poll, which keeps rotation, truncation and recovery testable with temp files.
/// </summary>
public sealed class SourceTailer
{
    private const int PreviousFileCandidates = 50;

    private readonly LogSource _source;
    private bool _startFromBeginning;

    /// <param name="cursor">Persisted position to resume from, or null for a new source (which starts at the end of its file).</param>
    public SourceTailer(LogSource source, FileCursor? cursor)
    {
        _source = source;
        Cursor = cursor;
    }

    public FileCursor? Cursor { get; private set; }

    public PollResult Poll(int maxBytes)
    {
        var lines = new List<string>();
        try
        {
            var target = ResolveTarget();
            if (target is null)
            {
                // A file that did not exist when the source was added is new content: read it from its start.
                if (Cursor is null) _startFromBeginning = true;
                return new PollResult(lines, SourceStatus.Unavailable, HasMore: false, "File or folder not found.");
            }

            Cursor ??= _startFromBeginning ? Fresh(target) : StartAtEnd(target);
            _startFromBeginning = false;

            var budget = maxBytes;
            if (!Reconcile(target, lines, ref budget))
            {
                return new PollResult(lines, SourceStatus.Active, HasMore: true, null);
            }

            using var stream = Open(target);
            var read = LineReader.Read(stream, Cursor.Offset, Cursor.Encoding, Math.Max(budget, 0), flushPartial: false);

            var fingerprint = Cursor.Fingerprint;
            if (FileFingerprint.CanExtend(fingerprint) && stream.Length > 0 && FileFingerprint.Matches(stream, fingerprint))
            {
                fingerprint = FileFingerprint.Compute(stream);
            }

            Cursor = new FileCursor(target, read.NewOffset, fingerprint, read.Encoding);
            lines.AddRange(read.Lines); // After the cursor moved, so a failure above cannot cause duplicates.
            return new PollResult(lines, SourceStatus.Active, HasMore: !read.ReachedEnd, null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            var missing = ex is FileNotFoundException or DirectoryNotFoundException;
            return new PollResult(lines, missing ? SourceStatus.Unavailable : SourceStatus.ReadError, HasMore: false, ex.Message);
        }
    }

    /// <summary>
    /// Makes the cursor point at <paramref name="target"/>: finishes a previous file (folder switch, or a file
    /// rotated away from the path and found by identity) and detects truncation. False when the budget ran out.
    /// </summary>
    private bool Reconcile(string target, List<string> lines, ref int budget)
    {
        while (true)
        {
            if (!string.Equals(Cursor!.Path, target, StringComparison.Ordinal))
            {
                if (!Drain(lines, ref budget)) return false;
                Cursor = Fresh(target);
                continue;
            }

            using var stream = Open(target);
            if (!FileFingerprint.Matches(stream, Cursor.Fingerprint))
            {
                // Replaced or rotated: the old content may still be reachable under another name.
                var previous = FindPrevious(target);
                if (previous is null)
                {
                    Cursor = Fresh(target);
                    return true;
                }

                Cursor = Cursor with { Path = previous };
                continue;
            }

            if (stream.Length < Cursor.Offset) Cursor = Fresh(target);
            return true;
        }
    }

    /// <summary>Reads what is left of the file the cursor points at. True when that file is finished with.</summary>
    private bool Drain(List<string> lines, ref int budget)
    {
        var path = Cursor!.Path;
        if (!File.Exists(path)) return true;

        using var stream = Open(path);
        if (stream.Length < Cursor.Offset || !FileFingerprint.Matches(stream, Cursor.Fingerprint)) return true;

        var read = LineReader.Read(stream, Cursor.Offset, Cursor.Encoding, Math.Max(budget, 0), flushPartial: true);
        lines.AddRange(read.Lines);
        budget -= (int)read.BytesRead;
        Cursor = Cursor with { Offset = read.NewOffset, Encoding = read.Encoding };
        return read.ReachedEnd;
    }

    private string? FindPrevious(string target)
    {
        if (Cursor?.Fingerprint is null) return null;

        var directory = Path.GetDirectoryName(target);
        if (string.IsNullOrEmpty(directory)) return null;

        var candidates = new DirectoryInfo(directory).EnumerateFiles()
            .Where(f => !string.Equals(f.FullName, target, StringComparison.Ordinal))
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .Take(PreviousFileCandidates);

        foreach (var candidate in candidates)
        {
            try
            {
                using var stream = Open(candidate.FullName);
                if (stream.Length >= Cursor.Offset && FileFingerprint.Matches(stream, Cursor.Fingerprint))
                {
                    return candidate.FullName;
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Not readable: not the file we are looking for.
            }
        }

        return null;
    }

    private string? ResolveTarget()
    {
        if (_source.Kind == SourceKind.File)
        {
            return File.Exists(_source.Path) ? _source.Path : null;
        }

        if (!Directory.Exists(_source.Path)) return null;

        // Daily files are named by date, so the newest write wins and the name breaks ties.
        return new DirectoryInfo(_source.Path)
            .EnumerateFiles(string.IsNullOrWhiteSpace(_source.Pattern) ? "*" : _source.Pattern)
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .ThenByDescending(f => f.Name, StringComparer.Ordinal)
            .FirstOrDefault()?.FullName;
    }

    private static FileCursor Fresh(string path) => new(path, 0, null, LineEncoding.Utf8);

    private static FileCursor StartAtEnd(string path)
    {
        using var stream = Open(path);
        if (stream.Length == 0) return Fresh(path);

        var (encoding, bomEnd) = LineReader.DetectBom(stream);
        var offset = stream.Length;
        if (encoding != LineEncoding.Utf8) offset &= ~1L;
        return new FileCursor(path, Math.Max(offset, bomEnd), FileFingerprint.Compute(stream), encoding);
    }

    /// <summary>Opened shared with writers and deleters so the monitored application is never blocked.</summary>
    private static FileStream Open(string path) =>
        new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, bufferSize: 1);
}
