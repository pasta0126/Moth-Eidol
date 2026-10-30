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
        new Migration(3, """
            CREATE TABLE projects (
                id                INTEGER PRIMARY KEY AUTOINCREMENT,
                name              TEXT NOT NULL COLLATE NOCASE UNIQUE,
                enabled           INTEGER NOT NULL DEFAULT 1,
                warning_alerts    INTEGER NOT NULL DEFAULT 0,
                frequency_mode    TEXT NOT NULL DEFAULT 'Immediate',
                frequency_minutes INTEGER NOT NULL DEFAULT 5
            );
            ALTER TABLE log_sources ADD COLUMN project_id INTEGER NOT NULL DEFAULT 0;
            -- Sources that existed before projects go into one project, keeping their read positions.
            INSERT INTO projects (name) SELECT 'Unassigned' WHERE EXISTS (SELECT 1 FROM log_sources);
            UPDATE log_sources
               SET project_id = (SELECT id FROM projects WHERE name = 'Unassigned')
             WHERE EXISTS (SELECT 1 FROM projects WHERE name = 'Unassigned');
            """),
    ];
}
