namespace MothEidol.Core.Alerts;

public static class RelativeTime
{
    /// <summary>"just now", "3 min ago", "2 h ago", "yesterday", and the date after that.</summary>
    public static string Format(DateTimeOffset then, DateTimeOffset now)
    {
        var age = now - then;
        if (age < TimeSpan.FromMinutes(1)) return "just now";
        if (age < TimeSpan.FromHours(1)) return $"{(int)age.TotalMinutes} min ago";
        if (age < TimeSpan.FromHours(24)) return $"{(int)age.TotalHours} h ago";
        if (age < TimeSpan.FromHours(48)) return "yesterday";
        return then.ToLocalTime().ToString("yyyy-MM-dd");
    }
}
