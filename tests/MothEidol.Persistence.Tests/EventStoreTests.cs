using Microsoft.Data.Sqlite;
using MothEidol.Core.Alerts;
using MothEidol.Core.Logs;
using MothEidol.Persistence;

namespace MothEidol.Persistence.Tests;

/// <summary>The behavior every event store must have, run against the in-memory and the SQLite one.</summary>
public abstract class EventStoreContract : IDisposable
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    protected abstract IEventStore Create();

    public virtual void Dispose() { }

    private static EventGroup Record(IEventStore store, string key = "k1", int second = 0, long project = 1,
        EntryLevel level = EntryLevel.Error, string message = "boom", int max = 50) =>
        store.Record(project, level, key, message.Split('\n')[0], 10, T0.AddSeconds(second), message, max);

    [Fact]
    public void The_first_occurrence_creates_a_pending_group()
    {
        var group = Record(Create());

        Assert.Equal(1, group.Count);
        Assert.Equal(1, group.PendingCount);
        Assert.True(group.IsPending);
        Assert.Equal(T0, group.FirstSeen);
        Assert.Equal(T0, group.LastSeen);
        Assert.Null(group.SeenAt);
    }

    [Fact]
    public void Further_occurrences_of_the_same_key_join_the_group()
    {
        var store = Create();
        Record(store, second: 0);
        Record(store, second: 30);
        var group = Record(store, second: 10, message: "boom with other id");

        Assert.Equal(3, group.Count);
        Assert.Equal(3, group.PendingCount);
        Assert.Equal(T0, group.FirstSeen);
        Assert.Equal(T0.AddSeconds(30), group.LastSeen);
        Assert.Equal("boom with other id", group.Title);
        Assert.Single(store.Groups(1));
    }

    [Fact]
    public void Different_keys_or_projects_are_different_groups()
    {
        var store = Create();
        Record(store, "a");
        Record(store, "b");
        Record(store, "a", project: 2);

        Assert.Equal(2, store.Groups(1).Count);
        Assert.Single(store.Groups(2));
    }

    [Fact]
    public void Only_the_latest_occurrences_keep_their_text_but_the_count_keeps_growing()
    {
        var store = Create();
        for (var i = 1; i <= 5; i++) Record(store, second: i, message: $"occurrence {i}", max: 3);

        var group = store.Groups(1).Single();
        Assert.Equal(5, group.Count);
        Assert.Equal(["occurrence 5", "occurrence 4", "occurrence 3"], store.Occurrences(group.Id).Select(o => o.Message));
    }

    [Fact]
    public void Occurrences_keep_their_full_multi_line_text_and_source()
    {
        var store = Create();
        var text = "Failed\nSystem.IO.IOException: disk\n   at A.B() in C:\\x.cs:line 5";

        var group = Record(store, message: text);

        var occurrence = Assert.Single(store.Occurrences(group.Id));
        Assert.Equal(text, occurrence.Message);
        Assert.Equal(10, occurrence.SourceId);
        Assert.Equal(T0, occurrence.At);
    }

    [Fact]
    public void Marking_a_group_as_seen_clears_its_pending_count_and_a_new_occurrence_makes_it_pending_again()
    {
        var store = Create();
        var group = Record(store);
        Record(store, second: 1);

        store.MarkSeen(group.Id, T0.AddMinutes(1));
        var seen = store.Groups(1).Single();
        Assert.Equal(0, seen.PendingCount);
        Assert.False(seen.IsPending);
        Assert.Equal(T0.AddMinutes(1), seen.SeenAt);
        Assert.Equal(2, seen.Count);

        var again = Record(store, second: 120);
        Assert.Equal(1, again.PendingCount);
        Assert.Equal(3, again.Count);
    }

    [Fact]
    public void Marking_everything_as_seen_only_affects_that_project()
    {
        var store = Create();
        Record(store, "a");
        Record(store, "b");
        Record(store, "a", project: 2);

        store.MarkAllSeen(1, T0.AddMinutes(1));

        Assert.All(store.Groups(1), g => Assert.False(g.IsPending));
        Assert.True(store.Groups(2).Single().IsPending);
    }

    [Fact]
    public void The_pending_summary_counts_occurrences_and_reports_the_worst_level()
    {
        var store = Create();
        Assert.Equal(PendingSummary.None, store.Pending(1));

        Record(store, "w", level: EntryLevel.Warning);
        Record(store, "w", level: EntryLevel.Warning);
        Assert.Equal(new PendingSummary(2, EntryLevel.Warning), store.Pending(1));

        Record(store, "e", level: EntryLevel.Error);
        Assert.Equal(new PendingSummary(3, EntryLevel.Error), store.Pending(1));

        store.MarkAllSeen(1, T0);
        Assert.Equal(PendingSummary.None, store.Pending(1));
    }

    [Fact]
    public void Purging_removes_quiet_seen_groups_and_never_pending_ones()
    {
        var store = Create();
        var oldSeen = Record(store, "old-seen", second: 0);
        Record(store, "old-pending", second: 0);
        var recentSeen = Record(store, "recent-seen", second: 3600);
        store.MarkSeen(oldSeen.Id, T0);
        store.MarkSeen(recentSeen.Id, T0);

        var removed = store.PurgeQuiet(T0.AddSeconds(1800));

        Assert.Equal(1, removed);
        Assert.Equal(2, store.Groups(1).Count);
        Assert.Empty(store.Occurrences(oldSeen.Id));
        Assert.DoesNotContain(store.Groups(1), g => g.Id == oldSeen.Id);
    }

    [Fact]
    public void Removing_a_project_removes_its_groups_and_occurrences()
    {
        var store = Create();
        var group = Record(store);
        Record(store, project: 2);

        store.RemoveProject(1);

        Assert.Empty(store.Groups(1));
        Assert.Empty(store.Occurrences(group.Id));
        Assert.Single(store.Groups(2));
    }

    [Fact]
    public void The_location_is_kept_and_the_latest_one_wins()
    {
        var store = Create();
        store.Record(1, EntryLevel.Error, "k", "t", 10, T0, "m", 50, location: "Worker.Run");
        var group = store.Record(1, EntryLevel.Error, "k", "t", 10, T0.AddSeconds(1), "m", 50, location: "Worker.Stop");

        Assert.Equal("Worker.Stop", group.Location);
        Assert.Equal("Worker.Stop", store.Groups(1).Single().Location);
    }

    [Fact]
    public void Context_entries_are_saved_with_the_occurrence_in_order()
    {
        var store = Create();
        var context = new List<ContextEntry>
        {
            new(T0.AddSeconds(-2), EntryLevel.Information, "Connecting"),
            new(T0.AddSeconds(-1), EntryLevel.Warning, "Retrying\n   at A.B()"),
        };

        var group = store.Record(1, EntryLevel.Error, "k", "t", 10, T0, "boom", 50, context: context);
        store.Record(1, EntryLevel.Error, "k", "t", 10, T0.AddSeconds(5), "boom again", 50);

        var occurrences = store.Occurrences(group.Id);
        Assert.Null(occurrences[0].Context); // no context for the second one
        Assert.Equal(context, occurrences[1].Context);
    }

    [Fact]
    public void Hourly_series_count_occurrences_per_hour_oldest_first_for_a_group_and_for_a_project()
    {
        var store = Create();
        var now = T0.AddMinutes(30); // 10:30
        var a = store.Record(1, EntryLevel.Error, "a", "a", 10, T0, "m", 50);
        store.Record(1, EntryLevel.Error, "a", "a", 10, T0.AddMinutes(10), "m", 50);
        store.Record(1, EntryLevel.Error, "a", "a", 10, T0.AddHours(-2), "m", 50);
        store.Record(1, EntryLevel.Error, "b", "b", 10, T0.AddHours(-1), "m", 50);
        store.Record(2, EntryLevel.Error, "c", "c", 10, T0, "m", 50);

        var group = store.GroupSeries(a.Id, now, 4);
        var project = store.ProjectSeries(1, now, 4);

        Assert.Equal([0, 1, 0, 2], group);          // 7h, 8h, 9h, 10h  -> 08:xx has one, 10:xx has two
        Assert.Equal([0, 1, 1, 2], project);        // plus the other group's occurrence at 09:xx
        Assert.Equal(4, store.ProjectSeries(1, now, 4).Count);
    }

    [Fact]
    public void Occurrences_outside_the_window_are_not_counted()
    {
        var store = Create();
        var group = store.Record(1, EntryLevel.Error, "a", "a", 10, T0.AddHours(-30), "m", 50);

        Assert.Equal(0, store.GroupSeries(group.Id, T0, 24).Sum());
        Assert.Equal(1, store.GroupSeries(group.Id, T0, 48).Sum());
    }

    [Fact]
    public void Old_hourly_counts_can_be_pruned_without_touching_recent_ones()
    {
        var store = Create();
        var group = store.Record(1, EntryLevel.Error, "a", "a", 10, T0.AddDays(-10), "m", 50);
        store.Record(1, EntryLevel.Error, "a", "a", 10, T0, "m", 50);

        Assert.Equal(1, store.PruneBuckets(T0.AddDays(-8)));

        Assert.Equal(0, store.GroupSeries(group.Id, T0, 24 * 11).Skip(0).Take(24 * 3).Sum());
        Assert.Equal(1, store.GroupSeries(group.Id, T0, 24).Sum());
    }

    [Fact]
    public void Purging_a_group_or_removing_a_project_drops_its_hourly_counts()
    {
        var store = Create();
        var purged = store.Record(1, EntryLevel.Error, "a", "a", 10, T0, "m", 50);
        store.MarkSeen(purged.Id, T0);
        var removed = store.Record(2, EntryLevel.Error, "b", "b", 10, T0, "m", 50);

        store.PurgeQuiet(T0.AddHours(1));
        store.RemoveProject(2);

        Assert.Equal(0, store.GroupSeries(purged.Id, T0, 24).Sum());
        Assert.Equal(0, store.ProjectSeries(1, T0, 24).Sum());
        Assert.Equal(0, store.GroupSeries(removed.Id, T0, 24).Sum());
        Assert.Equal(0, store.ProjectSeries(2, T0, 24).Sum());
    }

    [Fact]
    public void Display_order_puts_pending_first_and_then_the_most_recent()
    {
        var store = Create();
        var a = Record(store, "a", second: 0);
        Record(store, "b", second: 10);
        Record(store, "c", second: 20);
        store.MarkSeen(a.Id, T0);
        store.MarkSeen(store.Groups(1).Single(g => g.Title == "boom" && g.LastSeen == T0.AddSeconds(20)).Id, T0);

        var ordered = EventGroupOrder.ForDisplay(store.Groups(1));

        Assert.Equal([T0.AddSeconds(10), T0.AddSeconds(20), T0], ordered.Select(g => g.LastSeen));
    }
}

public sealed class InMemoryEventStoreTests : EventStoreContract
{
    protected override IEventStore Create() => new InMemoryEventStore();
}

public sealed class SqliteEventStoreTests : EventStoreContract
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "moth-eidol-tests-" + Guid.NewGuid().ToString("N"));

    protected override IEventStore Create() =>
        new SqliteEventStore(SqliteDatabase.Open(Path.Combine(_dir, "test.db")));

    public override void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void Groups_and_their_pending_state_survive_a_restart()
    {
        var path = Path.Combine(_dir, "restart.db");
        var first = new SqliteEventStore(SqliteDatabase.Open(path));
        var group = first.Record(1, EntryLevel.Error, "k", "boom", 10, new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero), "boom\n   at A()", 50);
        first.Record(1, EntryLevel.Error, "k", "boom", 10, new DateTimeOffset(2026, 1, 1, 10, 5, 0, TimeSpan.Zero), "boom\n   at A()", 50);
        first.MarkSeen(group.Id, new DateTimeOffset(2026, 1, 1, 10, 6, 0, TimeSpan.Zero));
        first.Record(1, EntryLevel.Error, "k", "boom", 10, new DateTimeOffset(2026, 1, 1, 11, 0, 0, TimeSpan.Zero), "boom\n   at A()", 50);

        var second = new SqliteEventStore(SqliteDatabase.Open(path));

        var restored = second.Groups(1).Single();
        Assert.Equal(3, restored.Count);
        Assert.Equal(1, restored.PendingCount);
        Assert.NotNull(restored.SeenAt);
        Assert.Equal(3, second.Occurrences(restored.Id).Count);
        Assert.Equal(3, second.GroupSeries(restored.Id, new DateTimeOffset(2026, 1, 1, 11, 30, 0, TimeSpan.Zero), 24).Sum());
    }

    [Fact]
    public void A_version_four_database_is_migrated_keeping_its_events()
    {
        var path = Path.Combine(_dir, "v4.db");
        var v4 = SqliteDatabase.Open(path, Migrations.All.Take(4).ToList());
        using (var connection = v4.CreateConnection())
        using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                INSERT INTO event_groups (id, project_id, level, key, title, count, first_seen, last_seen, pending_count)
                VALUES (1, 1, 5, 'k', 'old event', 3, 1000, 2000, 3);
                INSERT INTO event_occurrences (group_id, source_id, at, message) VALUES (1, 10, 2000, 'old message');
                """;
            command.ExecuteNonQuery();
        }

        var current = SqliteDatabase.Open(path);

        Assert.Equal(DatabaseStatus.Ready, current.Status);
        var store = new SqliteEventStore(current);
        var group = store.Groups(1).Single();
        Assert.Equal("old event", group.Title);
        Assert.Null(group.Location);
        var occurrence = Assert.Single(store.Occurrences(1));
        Assert.Equal("old message", occurrence.Message);
        Assert.Null(occurrence.Context);
        Assert.Equal(0, store.GroupSeries(1, DateTimeOffset.UtcNow, 24).Sum());
    }

    [Fact]
    public void A_version_three_database_is_migrated_keeping_its_projects()
    {
        var path = Path.Combine(_dir, "v3.db");
        var v3 = SqliteDatabase.Open(path, Migrations.All.Take(3).ToList());
        using (var connection = v3.CreateConnection())
        {
            using var insert = connection.CreateCommand();
            insert.CommandText = "INSERT INTO projects (name) VALUES ('Billing');";
            insert.ExecuteNonQuery();
        }

        var current = SqliteDatabase.Open(path);

        Assert.Equal(DatabaseStatus.Ready, current.Status);
        Assert.Single(new SqliteProjectRepository(current).List());
        Assert.Empty(new SqliteEventStore(current).Groups(1));
    }
}
