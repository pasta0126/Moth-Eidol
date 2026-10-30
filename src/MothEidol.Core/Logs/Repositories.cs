namespace MothEidol.Core.Logs;

public enum LineEncoding
{
    Utf8,
    Utf16Le,
    Utf16Be,
}

/// <summary>
/// Where reading stopped: the file followed, the byte offset of the next unread line, the encoding detected
/// at the start of the file and a fingerprint of the file's first bytes used to recognize the same file.
/// </summary>
public sealed record FileCursor(string Path, long Offset, string? Fingerprint, LineEncoding Encoding);

public interface ILogSourceRepository
{
    IReadOnlyList<LogSource> List();

    LogSource Add(SourceKind kind, string path, string? pattern);

    void Remove(long id);

    void SetEnabled(long id, bool enabled);
}

public interface IReadPositionStore
{
    FileCursor? Get(long sourceId);

    void Save(long sourceId, FileCursor cursor);

    void Delete(long sourceId);
}
