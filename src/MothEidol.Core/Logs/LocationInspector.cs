namespace MothEidol.Core.Logs;

public enum InspectionOutcome
{
    /// <summary>The location can be read.</summary>
    Ok,

    /// <summary>The file or folder does not exist.</summary>
    Missing,

    /// <summary>It exists but the OS user cannot read it.</summary>
    Unreadable,

    /// <summary>The input itself is not usable (for example an invalid pattern).</summary>
    Invalid,
}

public sealed record InspectedFile(string Name, string FullPath, long Length, DateTimeOffset Modified);

/// <param name="Files">Matching files, newest first, capped at <see cref="LocationInspector.MaxListedFiles"/>.</param>
/// <param name="TotalFiles">How many files matched in all.</param>
/// <param name="Sample">The latest recognized entries of the newest file, oldest first.</param>
/// <param name="Warnings">Problems that do not prevent adding the source (no matches, empty file, unknown format).</param>
public sealed record LocationInspection(
    InspectionOutcome Outcome,
    string? Error,
    IReadOnlyList<InspectedFile> Files,
    int TotalFiles,
    IReadOnlyList<LogEntry> Sample,
    IReadOnlyList<string> Warnings)
{
    public bool CanAdd => Outcome == InspectionOutcome.Ok;

    public InspectedFile? Newest => Files.Count > 0 ? Files[0] : null;
}

/// <summary>
/// Looks at a file or folder + pattern the way the tailer would, without following it: what matches, which
/// file would be followed and how its latest entries parse. Only the end of the file is read, so it stays
/// cheap on network shares.
/// </summary>
public static class LocationInspector
{
    public const int MaxListedFiles = 20;
    public const int TailBytes = 64 * 1024;

    public static LocationInspection Inspect(SourceKind kind, string path, string? pattern, int maxSample = 8)
    {
        path = path?.Trim() ?? "";
        if (path.Length == 0) return Failed(InspectionOutcome.Invalid, "Enter a path.");

        List<InspectedFile> files;
        try
        {
            if (kind == SourceKind.File)
            {
                if (Directory.Exists(path)) return Failed(InspectionOutcome.Invalid, "That path is a folder; choose a file or switch to a folder source.");
                if (!File.Exists(path)) return Failed(InspectionOutcome.Missing, "The file does not exist.");
                files = [Describe(new FileInfo(path))];
            }
            else
            {
                if (File.Exists(path)) return Failed(InspectionOutcome.Invalid, "That path is a file; choose a folder or switch to a file source.");
                if (!Directory.Exists(path)) return Failed(InspectionOutcome.Missing, "The folder does not exist.");
                if (string.IsNullOrWhiteSpace(pattern)) return Failed(InspectionOutcome.Invalid, "A folder needs a file-name pattern such as *.log.");

                // Same order the tailer uses to choose the file to follow.
                files = [.. new DirectoryInfo(path).EnumerateFiles(pattern.Trim())
                    .OrderByDescending(f => f.LastWriteTimeUtc)
                    .ThenByDescending(f => f.Name, StringComparer.Ordinal)
                    .Select(Describe)];
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            return Failed(InspectionOutcome.Unreadable, $"No permission to read it: {ex.Message}");
        }
        catch (ArgumentException ex)
        {
            return Failed(InspectionOutcome.Invalid, $"The pattern is not valid: {ex.Message}");
        }
        catch (IOException ex)
        {
            return Failed(InspectionOutcome.Unreadable, $"It could not be read: {ex.Message}");
        }

        var warnings = new List<string>();
        if (files.Count == 0)
        {
            warnings.Add("No file matches the pattern yet. You can add it anyway; monitoring starts when a file appears.");
            return new LocationInspection(InspectionOutcome.Ok, null, [], 0, [], warnings);
        }

        IReadOnlyList<LogEntry> sample = [];
        try
        {
            sample = ReadSample(files[0].FullPath, maxSample, out var hadLines);
            if (!hadLines) warnings.Add("The newest file is empty; entries will appear as it is written.");
            else if (sample.Count == 0) warnings.Add("No line was recognized as a log entry. Only Serilog text files are supported.");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Failed(InspectionOutcome.Unreadable, $"No permission to read the file: {ex.Message}");
        }
        catch (IOException ex)
        {
            return Failed(InspectionOutcome.Unreadable, $"The file could not be read: {ex.Message}");
        }

        return new LocationInspection(
            InspectionOutcome.Ok, null, [.. files.Take(MaxListedFiles)], files.Count, sample, warnings);
    }

    private static List<LogEntry> ReadSampleEntries(List<string> lines, int maxSample)
    {
        var assembler = new EntryAssembler(0, SerilogTextParser.Instance, new EntryOptions());
        var now = DateTimeOffset.UtcNow;
        var entries = new List<LogEntry>();
        foreach (var line in lines)
        {
            if (assembler.Add(line, now) is { } entry) entries.Add(entry);
        }
        if (assembler.FlushIfIdle(now + TimeSpan.FromDays(1)) is { } last) entries.Add(last);

        // Lines before the first header (a cut-off stack trace) have no level and are not entries of the format.
        return [.. entries.Where(e => e.Level != EntryLevel.Unknown || e.Timestamp is not null).TakeLast(maxSample)];
    }

    private static List<LogEntry> ReadSample(string path, int maxSample, out bool hadLines)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, bufferSize: 1);
        var (encoding, bomEnd) = LineReader.DetectBom(stream);
        var start = Math.Max(bomEnd, stream.Length - TailBytes);
        if (encoding != LineEncoding.Utf8) start &= ~1L;

        var read = LineReader.Read(stream, start, encoding, TailBytes, flushPartial: true);
        var lines = read.Lines;
        if (start > bomEnd && lines.Count > 0) lines.RemoveAt(0); // Started mid-line.

        hadLines = lines.Any(l => l.Length > 0);
        return ReadSampleEntries(lines, maxSample);
    }

    private static InspectedFile Describe(FileInfo file) =>
        new(file.Name, file.FullName, file.Length, new DateTimeOffset(file.LastWriteTimeUtc, TimeSpan.Zero));

    private static LocationInspection Failed(InspectionOutcome outcome, string error) =>
        new(outcome, error, [], 0, [], []);
}
