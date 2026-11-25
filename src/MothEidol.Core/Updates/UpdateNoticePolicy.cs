using System.Globalization;

namespace MothEidol.Core.Updates;

/// <summary>
/// Decides whether a published version is announced: only when the check is on, the version is newer than the
/// running one and the user has not dismissed that exact version. Versions compare on major.minor.patch only:
/// the Store's fourth part is always 0, and a development build x.y.z.a counts as x.y.z.
/// </summary>
public static class UpdateNoticePolicy
{
    /// <summary>The version to announce, or null when nothing is announced.</summary>
    public static Version? Decide(bool checkEnabled, string runningVersion, string? dismissedVersion, UpdateCheckResult result)
    {
        if (!checkEnabled || result is not UpdateCheckResult.Available available) return null;

        var published = Release(available.Version);
        var running = Parse(runningVersion);
        if (running is null || published <= running) return null;

        var dismissed = Parse(dismissedVersion);
        return dismissed is not null && dismissed == published ? null : published;
    }

    /// <summary>The x.y.z part of a version, as stored for a dismissal.</summary>
    public static Version Release(Version version) =>
        new(version.Major, Math.Max(version.Minor, 0), Math.Max(version.Build, 0));

    /// <summary>Parses "x.y.z", "x.y.z.a" or "x.y.z-pre" into x.y.z; null when unreadable.</summary>
    public static Version? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var core = text.Trim();
        var cut = core.IndexOfAny(['-', '+']);
        if (cut >= 0) core = core[..cut];
        return Version.TryParse(core, out var version) ? Release(version) : null;
    }

    public static string Format(Version version) =>
        string.Create(CultureInfo.InvariantCulture, $"{version.Major}.{version.Minor}.{version.Build}");
}
