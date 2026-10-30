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
    }

    [Fact]
    public void A_version_three_database_is_migrated_keeping_its_projects()
    {
        var path = Path.Combine(_dir, "v3.db");
        var v3 = SqliteDatabase.Open(path, Migrations.All.Take(3).ToList());
        new SqliteProjectRepository(v3).Add("Billing");

        var current = SqliteDatabase.Open(path);

        Assert.Equal(DatabaseStatus.Ready, current.Status);
        Assert.Single(new SqliteProjectRepository(current).List());
        Assert.Empty(new SqliteEventStore(current).Groups(1));
    }
}
