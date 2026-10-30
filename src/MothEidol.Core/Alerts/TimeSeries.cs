namespace MothEidol.Core.Alerts;

public enum TrendDirection { None, Rising, Falling, Same }

/// <summary>Activity of the last 24 hours against the 24 hours before.</summary>
public sealed record Trend(int Last24Hours, int Previous24Hours)
{
    public TrendDirection Direction =>
        Last24Hours == 0 && Previous24Hours == 0 ? TrendDirection.None
        : Last24Hours > Previous24Hours ? TrendDirection.Rising
        : Last24Hours < Previous24Hours ? TrendDirection.Falling
        : TrendDirection.Same;

    /// <summary>Splits 48 hourly counts (oldest first) into the two periods.</summary>
    public static Trend From(IReadOnlyList<int> last48Hours)
    {
        var previous = last48Hours.Take(24).Sum();
        var last = last48Hours.Skip(24).Sum();
        return new Trend(last, previous);
    }
}

public static class TimeSeries
{
    /// <summary>Sums hourly counts (oldest first, the last one being the current hour) into local calendar days.</summary>
    public static IReadOnlyList<int> ByDay(IReadOnlyList<int> hourly, DateTimeOffset now, int days)
    {
        var result = new int[days];
        var today = now.ToLocalTime().Date;
        for (var i = 0; i < hourly.Count; i++)
        {
            var hoursAgo = hourly.Count - 1 - i;
            var day = now.AddHours(-hoursAgo).ToLocalTime().Date;
            var index = days - 1 - (int)(today - day).TotalDays;
            if (index >= 0 && index < days) result[index] += hourly[i];
        }
        return result;
    }
}
