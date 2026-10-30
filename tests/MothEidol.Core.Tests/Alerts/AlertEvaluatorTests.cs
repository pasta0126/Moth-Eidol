using MothEidol.Core.Alerts;
using MothEidol.Core.Logs;
using MothEidol.Core.Projects;

namespace MothEidol.Core.Tests.Alerts;

public class AlertEvaluatorTests
{
    [Theory]
    [InlineData(EntryLevel.Fatal, false, true)]
    [InlineData(EntryLevel.Fatal, true, true)]
    [InlineData(EntryLevel.Error, false, true)]
    [InlineData(EntryLevel.Error, true, true)]
    [InlineData(EntryLevel.Warning, false, false)]
    [InlineData(EntryLevel.Warning, true, true)]
    [InlineData(EntryLevel.Information, false, false)]
    [InlineData(EntryLevel.Information, true, false)]
    [InlineData(EntryLevel.Debug, true, false)]
    [InlineData(EntryLevel.Verbose, true, false)]
    [InlineData(EntryLevel.Unknown, false, false)]
    [InlineData(EntryLevel.Unknown, true, false)]
    public void Only_errors_always_alert_and_warnings_follow_the_project_setting(
        EntryLevel level, bool warningAlerts, bool expected)
    {
        var settings = AlertSettings.Default with { WarningAlerts = warningAlerts };

        Assert.Equal(expected, AlertEvaluator.IsAlertLevel(level, settings));
    }
}
