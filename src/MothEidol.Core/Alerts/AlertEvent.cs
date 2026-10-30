using MothEidol.Core.Logs;
using MothEidol.Core.Projects;

namespace MothEidol.Core.Alerts;

/// <summary>An alert-level entry of a project, as seen by the delivery channels.</summary>
public sealed record AlertEvent(
    long ProjectId, long SourceId, EntryLevel Level, string Message, DateTimeOffset At);

public static class AlertEvaluator
{
    /// <summary>
    /// Error and above always alert, information and below never do, and warnings alert only when the
    /// project asks for them. Entries with an unknown level never alert.
    /// </summary>
    public static bool IsAlertLevel(EntryLevel level, AlertSettings settings) => level switch
    {
        EntryLevel.Error or EntryLevel.Fatal => true,
        EntryLevel.Warning => settings.WarningAlerts,
        _ => false,
    };
}
