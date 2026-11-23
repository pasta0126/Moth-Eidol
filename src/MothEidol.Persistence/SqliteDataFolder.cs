using MothEidol.Core.Projects;

namespace MothEidol.Persistence;

/// <summary>
/// The folder of the local database. Only the copies of a corrupt database (named after the database file plus
/// <see cref="SqliteDatabase.CorruptBackupMarker"/>) are ever listed or deleted; the live file and its
/// <c>-wal</c>/<c>-shm</c> files never match.
/// </summary>
public sealed class SqliteDataFolder(string databasePath) : IDataFolder
{
    private readonly string _directory = Path.GetDirectoryName(Path.GetFullPath(databasePath)) ?? ".";
    private readonly string _prefix = Path.GetFileName(databasePath) + SqliteDatabase.CorruptBackupMarker;

    public string? Location => _directory;

    public IReadOnlyList<DataFile> CorruptBackups()
    {
        if (!Directory.Exists(_directory)) return [];
        return
        [
            .. new DirectoryInfo(_directory).EnumerateFiles(_prefix + "*")
                .Where(f => IsBackup(f.FullName))
                .OrderBy(f => f.Name, StringComparer.Ordinal)
                .Select(f => new DataFile(f.Name, f.FullName, f.Length)),
        ];
    }

    public void Delete(DataFile file)
    {
        if (!IsBackup(file.Path)) throw new InvalidOperationException($"{file.Name} is not a copy of a corrupt database.");
        File.Delete(file.Path);
    }

    private bool IsBackup(string path)
    {
        var full = Path.GetFullPath(path);
        return string.Equals(Path.GetDirectoryName(full), _directory, StringComparison.Ordinal)
               && Path.GetFileName(full).StartsWith(_prefix, StringComparison.Ordinal)
               && Path.GetFileName(full).Length > _prefix.Length;
    }
}
