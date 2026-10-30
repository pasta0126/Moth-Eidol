namespace MothEidol.Core.Logs;

/// <summary>Raw lines emitted by the sources.</summary>
public sealed class LineBus : Broadcast<LogLine>;

/// <summary>Parsed entries (a header plus its continuation lines) assembled from the lines.</summary>
public sealed class EntryBus : Broadcast<LogEntry>;
