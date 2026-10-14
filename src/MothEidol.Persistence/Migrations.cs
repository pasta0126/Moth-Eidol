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
    ];
}
