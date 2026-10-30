using MothEidol.Core.Alerts;
using MothEidol.Core.Logs;

namespace MothEidol.Core.Tests.Alerts;

public class EventDescriberTests
{
    [Fact]
    public void An_exception_gives_its_type_and_message_as_the_title_and_the_first_application_frame_as_the_location()
    {
        var description = EventDescriber.Describe(
            "Failed to send\nSystem.InvalidOperationException: boom\n" +
            "   at System.Threading.Tasks.Task.Run()\n" +
            "   at DocumentFetcher.Services.Worker.ExecuteAsync(CancellationToken token) in C:\\src\\Worker.cs:line 60");

        Assert.Equal("InvalidOperationException: boom", description.Title);
        Assert.Equal("Worker.ExecuteAsync", description.Location);
    }

    [Fact]
    public void Without_an_exception_the_title_is_the_first_line_and_there_is_no_location()
    {
        var description = EventDescriber.Describe("Envío KO: registro 12\nsecond line");

        Assert.Equal("Envío KO: registro 12", description.Title);
        Assert.Null(description.Location);
    }

    [Fact]
    public void With_only_framework_frames_there_is_no_location()
    {
        var description = EventDescriber.Describe(
            "Failed\nSystem.IO.IOException: disk full\n   at System.IO.File.WriteAllBytes()\n   at System.Threading.Tasks.Task.Run()");

        Assert.Equal("IOException: disk full", description.Title);
        Assert.Null(description.Location);
    }

    [Fact]
    public void An_exception_without_a_message_uses_just_the_type()
    {
        Assert.Equal("NullReferenceException", EventDescriber.Describe("Failed\nSystem.NullReferenceException").Title);
    }

    [Theory]
    [InlineData("Acme.Billing.Worker.Run(String a, Int32 b)", "Worker.Run")]
    [InlineData("Run()", "Run")]
    [InlineData("Acme.Repo`1.Load[T](T key)", "Repo.Load")]
    [InlineData("ArchiveCleanupService.Worker.<ExecuteAsync>d__2.MoveNext()", "Worker.ExecuteAsync")]
    public void Locations_read_as_Class_dot_Method(string method, string expected)
    {
        Assert.Equal(expected, EventDescriber.Location(method));
    }
}

public class RelativeTimeTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(10, "just now")]
    [InlineData(59, "just now")]
    [InlineData(180, "3 min ago")]
    [InlineData(3599, "59 min ago")]
    [InlineData(3600, "1 h ago")]
    [InlineData(2 * 3600 + 5, "2 h ago")]
    [InlineData(23 * 3600, "23 h ago")]
    [InlineData(30 * 3600, "yesterday")]
    public void Recent_times_read_naturally(int secondsAgo, string expected)
    {
        Assert.Equal(expected, RelativeTime.Format(Now.AddSeconds(-secondsAgo), Now));
    }

    [Fact]
    public void Older_times_show_the_date()
    {
        var then = Now.AddDays(-5);

        Assert.Equal(then.ToLocalTime().ToString("yyyy-MM-dd"), RelativeTime.Format(then, Now));
    }
}

public class TrendAndSeriesTests
{
    [Fact]
    public void The_trend_compares_the_last_24_hours_with_the_24_before()
    {
        var rising = Trend.From([.. Enumerable.Repeat(1, 24), .. Enumerable.Repeat(2, 24)]);
        var falling = Trend.From([.. Enumerable.Repeat(3, 24), .. Enumerable.Repeat(1, 24)]);
        var same = Trend.From([.. Enumerable.Repeat(2, 48)]);
        var none = Trend.From(new int[48]);

        Assert.Equal((48, 24, TrendDirection.Rising), (rising.Last24Hours, rising.Previous24Hours, rising.Direction));
        Assert.Equal(TrendDirection.Falling, falling.Direction);
        Assert.Equal(TrendDirection.Same, same.Direction);
        Assert.Equal(TrendDirection.None, none.Direction);
    }

    [Fact]
    public void Hourly_counts_are_summed_into_local_days_ending_today()
    {
        var now = new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero);
        var hourly = new int[48];
        hourly[^1] = 4; // this hour
        hourly[^2] = 1; // the hour before
        hourly[0] = 7;  // 47 hours ago

        var days = TimeSeries.ByDay(hourly, now, 7);

        Assert.Equal(7, days.Count);
        Assert.Equal(12, days.Sum());
        Assert.True(days[^1] >= 4); // today holds at least the current hour
    }

    [Fact]
    public void New_and_regressed_flags_follow_first_seen_and_the_seen_mark()
    {
        var now = new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero);
        var fresh = new EventGroup(1, 1, EntryLevel.Error, "t", 1, now.AddHours(-2), now, null, 1);
        var old = fresh with { FirstSeen = now.AddHours(-30) };
        var regressed = old with { SeenAt = now.AddHours(-5), PendingCount = 2 };
        var seen = old with { SeenAt = now.AddHours(-5), PendingCount = 0 };

        Assert.True(fresh.IsNew(now));
        Assert.False(old.IsNew(now));
        Assert.False(fresh.IsRegressed);
        Assert.True(regressed.IsRegressed);
        Assert.False(seen.IsRegressed);
    }
}

public class EventSummaryTextTests
{
    [Fact]
    public void The_summary_has_project_title_location_counts_times_and_the_latest_stack_trace()
    {
        var at = new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero);
        var group = new EventGroup(1, 1, EntryLevel.Error, "InvalidOperationException: boom", 12, at.AddDays(-1), at, null, 5, "Worker.Run");
        var latest = new EventOccurrence(1, 1, 7, at, "Failed\nSystem.InvalidOperationException: boom\n   at Worker.Run()");

        var text = EventSummaryText.Build("Billing", group, latest);

        Assert.StartsWith("[ERROR] InvalidOperationException: boom", text);
        Assert.Contains("Location: Worker.Run", text);
        Assert.Contains("Project: Billing", text);
        Assert.Contains("Occurrences: 12 (5 pending)", text);
        Assert.Contains("First seen:", text);
        Assert.Contains("Last seen:", text);
        Assert.Contains("   at Worker.Run()", text);
    }

    [Fact]
    public void A_group_with_everything_seen_and_no_location_still_summarizes()
    {
        var at = DateTimeOffset.Now;
        var group = new EventGroup(1, 1, EntryLevel.Warning, "careful", 2, at, at, at, 0);

        var text = EventSummaryText.Build("Billing", group, null);

        Assert.Contains("all seen", text);
        Assert.DoesNotContain("Location:", text);
        Assert.DoesNotContain("Latest occurrence", text);
    }
}
