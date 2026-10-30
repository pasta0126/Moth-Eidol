namespace MothEidol.Core.Logs;

public enum SourceKind
{
    /// <summary>A single log file.</summary>
    File,

    /// <summary>A folder plus a file-name pattern; the newest matching file is followed.</summary>
    Folder,
}

public enum SourceStatus
{
    Active,
    /// <summary>The source is disabled and not being read.</summary>
    Paused,
    /// <summary>The file or folder does not exist or cannot be reached; retried periodically.</summary>
    Unavailable,
    /// <summary>Reading failed for another reason; retried periodically.</summary>
    ReadError,
}

/// <param name="Path">File path, or folder path for folder sources.</param>
/// <param name="Pattern">File-name glob for folder sources (for example <c>Service*.log</c>); null for file sources.</param>
public sealed record LogSource(long Id, SourceKind Kind, string Path, string? Pattern, bool Enabled)
{
    public string DisplayName => Kind == SourceKind.File
        ? Path
        : System.IO.Path.Combine(Path, Pattern ?? "*");
}

/// <summary>One complete line read from a source.</summary>
public sealed record LogLine(long SourceId, string Text, DateTimeOffset ReadAt);
