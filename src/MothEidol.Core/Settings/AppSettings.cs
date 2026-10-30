using System.Globalization;

namespace MothEidol.Core.Settings;

/// <summary>Typed access to user settings with their default values.</summary>
public sealed class AppSettings(ISettingsRepository repository)
{
    public static class Keys
    {
        public const string StartWithSession = "app.start_with_session";
        public const string PollIntervalMs = "tail.poll_interval_ms";
        public const string ViewerMaxLines = "viewer.max_lines";
        public const string EventRetentionDays = "alerts.retention_days";
    }

    public bool StartWithSession
    {
        get => GetBool(Keys.StartWithSession, defaultValue: false);
        set => SetBool(Keys.StartWithSession, value);
    }

    /// <summary>Polling interval of the log sources, in milliseconds (the worst-case latency of a new line).</summary>
    public int PollIntervalMs
    {
        get => GetInt(Keys.PollIntervalMs, defaultValue: 1000, min: 100);
        set => SetInt(Keys.PollIntervalMs, value);
    }

    /// <summary>Maximum number of lines the live viewer keeps per source.</summary>
    public int ViewerMaxLines
    {
        get => GetInt(Keys.ViewerMaxLines, defaultValue: 5000, min: 100);
        set => SetInt(Keys.ViewerMaxLines, value);
    }

    /// <summary>How long a seen event group is kept without new occurrences; pending groups are never removed automatically.</summary>
    public int EventRetentionDays
    {
        get => GetInt(Keys.EventRetentionDays, defaultValue: 30, min: 1);
        set => SetInt(Keys.EventRetentionDays, value);
    }

    private int GetInt(string key, int defaultValue, int min) =>
        int.TryParse(repository.Get(key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? Math.Max(value, min)
            : defaultValue;

    private void SetInt(string key, int value) =>
        repository.Set(key, value.ToString(CultureInfo.InvariantCulture));

    private bool GetBool(string key, bool defaultValue) =>
        bool.TryParse(repository.Get(key), out var value) ? value : defaultValue;

    private void SetBool(string key, bool value) =>
        repository.Set(key, value.ToString(CultureInfo.InvariantCulture));
}
