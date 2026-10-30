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
        new Migration(4, """
            CREATE TABLE event_groups (
                id            INTEGER PRIMARY KEY AUTOINCREMENT,
                project_id    INTEGER NOT NULL,
                level         INTEGER NOT NULL,
                key           TEXT NOT NULL,
                title         TEXT NOT NULL,
                count         INTEGER NOT NULL,
                first_seen    INTEGER NOT NULL,
                last_seen     INTEGER NOT NULL,
                seen_at       INTEGER NULL,
                pending_count INTEGER NOT NULL,
                UNIQUE (project_id, key)
            );
            CREATE INDEX ix_event_groups_project ON event_groups (project_id, last_seen DESC);
            CREATE TABLE event_occurrences (
                id        INTEGER PRIMARY KEY AUTOINCREMENT,
                group_id  INTEGER NOT NULL,
                source_id INTEGER NOT NULL,
                at        INTEGER NOT NULL,
                message   TEXT NOT NULL
            );
            CREATE INDEX ix_event_occurrences_group ON event_occurrences (group_id, id DESC);
            """),
        new Migration(5, """
            ALTER TABLE event_groups ADD COLUMN location TEXT NULL;
            ALTER TABLE event_occurrences ADD COLUMN context TEXT NULL;
            -- Occurrences per group per UTC hour (hour = unix seconds / 3600) for sparklines, trends and timelines.
            CREATE TABLE event_buckets (
                group_id   INTEGER NOT NULL,
                project_id INTEGER NOT NULL,
                hour       INTEGER NOT NULL,
                count      INTEGER NOT NULL,
                PRIMARY KEY (group_id, hour)
            );
            CREATE INDEX ix_event_buckets_project ON event_buckets (project_id, hour);
            """),
    ];
}
