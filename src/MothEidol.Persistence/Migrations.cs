namespace MothEidol.Persistence;

public static class Migrations
{
    public static IReadOnlyList<Migration> All { get; } =
    [
        new Migration(1, """
            CREATE TABLE settings (
                key   TEXT PRIMARY KEY NOT NULL,
                value TEXT NOT NULL
            );
            """),
        new Migration(2, """
            CREATE TABLE log_sources (
                id      INTEGER PRIMARY KEY AUTOINCREMENT,
                kind    TEXT NOT NULL,
                path    TEXT NOT NULL,
                pattern TEXT NULL,
                enabled INTEGER NOT NULL DEFAULT 1
            );
            CREATE TABLE read_positions (
                source_id   INTEGER PRIMARY KEY NOT NULL,
                file_path   TEXT NOT NULL,
                "offset"    INTEGER NOT NULL,
                fingerprint TEXT NULL,
                encoding    TEXT NOT NULL
            );
            """),
    ];
}
