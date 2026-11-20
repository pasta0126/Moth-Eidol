using MothEidol.Core.Alerts;

namespace MothEidol.Core.Projects;

/// <summary>What a project weighs in the local database.</summary>
/// <param name="ApproxBytes">Text stored for the project plus a fixed per-row overhead; an estimate, not the file share.</param>
/// <param name="Oldest">The oldest retained occurrence, or null when there is none.</param>
public sealed record ProjectStorageUsage(
    long Groups, long PendingGroups, long Occurrences, long HourlyCounts, long ApproxBytes,
    DateTimeOffset? Oldest, DateTimeOffset? Newest)
{
    public static ProjectStorageUsage Empty { get; } = new(0, 0, 0, 0, 0, null, null);

    public bool IsEmpty => Groups == 0 && Occurrences == 0 && HourlyCounts == 0;

    /// <summary>Per-row overheads added to the text lengths in the size estimate.</summary>
    public const int GroupOverhead = 40, OccurrenceOverhead = 40, BucketOverhead = 16;
}

/// <summary>Size of the whole database: the main file and its write-ahead log.</summary>
public sealed record DatabaseFileUsage(long MainBytes, long WalBytes)
{
    public static DatabaseFileUsage None { get; } = new(0, 0);

    public long TotalBytes => MainBytes + WalBytes;
}

/// <summary>What a cleanup removed.</summary>
public sealed record CleanupResult(int Groups, int Occurrences)
{
    public static CleanupResult Nothing { get; } = new(0, 0);
}

/// <summary>A project's storage measured against its threshold.</summary>
/// <param name="SizeBytes">The last measured approximate size, or null before the first measure.</param>
public sealed record StorageWarning(long? SizeBytes, int ThresholdMb)
{
    public long ThresholdBytes => ThresholdMb * 1024L * 1024L;

    /// <summary>Over the threshold; never before the size has been measured.</summary>
    public bool IsOver => SizeBytes is { } size && size > ThresholdBytes;

    /// <summary>The tile line while over the threshold ("Storage over 100 MB"), empty otherwise.</summary>
    public string TileText => IsOver ? $"Storage over {ThresholdMb:N0} MB" : "";
}

/// <summary>Measuring and lightening what each project keeps in the local database.</summary>
public interface IProjectStorage
{
    ProjectStorageUsage Usage(long projectId);

    DatabaseFileUsage File();

    /// <summary>Removes every event group of the project with its occurrences and hourly counts, pending ones included.</summary>
    CleanupResult ClearAll(long projectId);

    /// <summary>
    /// Removes the project's occurrences and hourly counts older than the cutoff and the groups with no activity
    /// since then (pending ones included). Kept groups keep their totals, first-seen date and pending state.
    /// </summary>
    CleanupResult KeepLastDays(long projectId, DateTimeOffset cutoff);

    /// <summary>Returns the space freed by deletions to the disk, so the file shrinks.</summary>
    void Compact();

    /// <summary>Truncates the write-ahead log and refreshes the query planner statistics.</summary>
    void Checkpoint();

    /// <summary>Removes rows of projects, sources or groups that no longer exist. Returns how many.</summary>
    int RemoveOrphans();
}

/// <summary>Storage of the non-persistent event store, used when the database cannot be used and in tests.</summary>
public sealed class InMemoryProjectStorage(InMemoryEventStore events) : IProjectStorage
{
    public ProjectStorageUsage Usage(long projectId) => events.UsageOf(projectId);

    public DatabaseFileUsage File() => DatabaseFileUsage.None;

    public CleanupResult ClearAll(long projectId) => events.ClearProject(projectId);

    public CleanupResult KeepLastDays(long projectId, DateTimeOffset cutoff) => events.KeepSince(projectId, cutoff);

    public void Compact() { }

    public void Checkpoint() { }

    public int RemoveOrphans() => 0;
}

public static class ByteSize
{
    /// <summary>"0 B", "512 B", "3.4 KB", "120.5 MB", "1.2 GB" (binary units).</summary>
    public static string Format(long bytes)
    {
        if (bytes < 1024) return $"{bytes:N0} B";
        string[] units = ["KB", "MB", "GB", "TB"];
        double value = bytes;
        var unit = -1;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }
        return $"{value:0.#} {units[unit]}";
    }
}
