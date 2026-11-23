namespace MothEidol.Core.Projects;

/// <summary>A file kept in the data folder, such as the copy of a database that was found corrupt.</summary>
public sealed record DataFile(string Name, string Path, long Bytes);

/// <summary>The folder that holds the local database, and the leftover files in it.</summary>
public interface IDataFolder
{
    /// <summary>Where the data lives, or null when nothing is stored on disk.</summary>
    string? Location { get; }

    /// <summary>Copies of a corrupt database kept aside when a new one was created.</summary>
    IReadOnlyList<DataFile> CorruptBackups();

    /// <summary>Deletes one of the <see cref="CorruptBackups"/>; any other file is refused.</summary>
    void Delete(DataFile file);
}

/// <summary>No data on disk (the database could not be used).</summary>
public sealed class NoDataFolder : IDataFolder
{
    public string? Location => null;

    public IReadOnlyList<DataFile> CorruptBackups() => [];

    public void Delete(DataFile file) => throw new InvalidOperationException("There is no data folder.");
}
