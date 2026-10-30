using MothEidol.Core.Logs;

namespace MothEidol.Core.Projects;

public enum ProjectHealth
{
    /// <summary>All monitored sources are being read.</summary>
    Healthy,

    /// <summary>A source is unavailable or failing to be read.</summary>
    Degraded,

    /// <summary>The project has no sources to monitor yet.</summary>
    NotConfigured,

    /// <summary>The project is switched off.</summary>
    Disabled,
}

public sealed record ProjectStatus(ProjectHealth Health, IReadOnlyList<string> AffectedSources)
{
    /// <summary>Derives the status from the project's sources and what each is currently doing.</summary>
    public static ProjectStatus Calculate(
        Project project, IReadOnlyList<LogSource> sources, Func<long, SourceStatus> statusOf)
    {
        if (!project.Enabled) return new ProjectStatus(ProjectHealth.Disabled, []);

        var monitored = sources.Where(s => s.Enabled).ToList();
        if (monitored.Count == 0) return new ProjectStatus(ProjectHealth.NotConfigured, []);

        var failing = monitored
            .Where(s => statusOf(s.Id) is SourceStatus.Unavailable or SourceStatus.ReadError)
            .Select(s => s.DisplayName)
            .ToList();

        return failing.Count > 0
            ? new ProjectStatus(ProjectHealth.Degraded, failing)
            : new ProjectStatus(ProjectHealth.Healthy, []);
    }
}
