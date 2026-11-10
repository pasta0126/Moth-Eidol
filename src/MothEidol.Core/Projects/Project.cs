namespace MothEidol.Core.Projects;

public enum FrequencyMode
{
    /// <summary>The first alert is delivered at once and further ones are grouped until the cooldown ends.</summary>
    Immediate,

    /// <summary>One aggregated alert per interval.</summary>
    Summary,
}

/// <summary>
/// How a project raises alerts. Error and above always alert and information never does, so only the
/// warning behavior and the delivery frequency are configurable.
/// </summary>
/// <param name="Minutes">The cooldown (immediate mode) or the interval (summary mode).</param>
public sealed record AlertSettings(bool WarningAlerts, FrequencyMode Mode, int Minutes)
{
    public const int MaxMinutes = 24 * 60;

    public static AlertSettings Default { get; } = new(WarningAlerts: false, FrequencyMode.Immediate, Minutes: 5);

    public void Validate()
    {
        if (Minutes is < 1 or > MaxMinutes)
            throw new ArgumentException($"The cooldown or interval must be between 1 and {MaxMinutes} minutes.");
    }
}

/// <param name="AccentColor">A normalized <c>#RRGGBB</c> color chosen by the user, or null for the default accent.</param>
/// <param name="Position">Place in the user's order of the dashboard tiles (lower first).</param>
public sealed record Project(
    long Id, string Name, bool Enabled, AlertSettings Alerts, string? AccentColor = null, int Position = 0);

public interface IProjectRepository
{
    /// <summary>All projects in the user's order (by position).</summary>
    IReadOnlyList<Project> List();

    /// <summary>Creates a project placed after all the others.</summary>

    Project Add(string name);

    void Update(Project project);

    void Remove(long id);

    /// <summary>
    /// Stores the order given: the listed projects first, in that order, then any not listed keeping their
    /// current relative order. All positions change together or none does.
    /// </summary>
    void Reorder(IReadOnlyList<long> orderedIds);
}
