using MothEidol.Core.Alerts;
using MothEidol.Core.Logs;
using MothEidol.Core.Projects;

namespace MothEidol.Core.Tests.Projects;

public sealed class TileStatusTests
{
    private static readonly ProjectStatus Healthy = new(ProjectHealth.Healthy, []);
    private static readonly ProjectStatus Degraded = new(ProjectHealth.Degraded, ["app.log"]);
    private static readonly PendingSummary Warnings = new(3, EntryLevel.Warning);
    private static readonly PendingSummary Errors = new(12, EntryLevel.Error);

    [Fact]
    public void Healthy_with_nothing_pending_is_green_without_detail()
    {
        var tile = TileStatus.From(Healthy, PendingSummary.None);

        Assert.Equal(StatusLight.Green, tile.Light);
        Assert.Equal("Healthy", tile.Text);
        Assert.Equal("", tile.Detail);
    }

    [Fact]
    public void Pending_warnings_are_yellow_with_the_count()
    {
        var tile = TileStatus.From(Healthy, Warnings);

        Assert.Equal(StatusLight.Yellow, tile.Light);
        Assert.Equal("3 pending", tile.Text);
        Assert.Equal("Warnings to review", tile.Detail);
    }

    [Fact]
    public void Pending_errors_are_red_with_the_count()
    {
        var tile = TileStatus.From(Healthy, Errors);

        Assert.Equal(StatusLight.Red, tile.Light);
        Assert.Equal("12 pending", tile.Text);
        Assert.Equal("Errors to review", tile.Detail);
    }

    [Fact]
    public void A_source_down_is_orange_and_names_the_source()
    {
        var tile = TileStatus.From(Degraded, PendingSummary.None);

        Assert.Equal(StatusLight.Orange, tile.Light);
        Assert.Equal("Degraded", tile.Text);
        Assert.Equal("Unavailable: app.log", tile.Detail);
    }

    [Fact]
    public void Several_sources_down_name_the_first_and_count_the_rest()
    {
        var tile = TileStatus.From(new ProjectStatus(ProjectHealth.Degraded, ["a.log", "b.log", "c.log"]), PendingSummary.None);

        Assert.Equal("Unavailable: a.log and 2 more", tile.Detail);
    }

    [Fact]
    public void Orange_outweighs_yellow_and_keeps_the_pending_warnings_in_the_detail()
    {
        var tile = TileStatus.From(Degraded, Warnings);

        Assert.Equal(StatusLight.Orange, tile.Light);
        Assert.Equal("Degraded", tile.Text);
        Assert.Equal("Unavailable: app.log · 3 pending (warnings)", tile.Detail);
    }

    [Fact]
    public void Red_outweighs_orange_and_keeps_the_unavailable_source_in_the_detail()
    {
        var tile = TileStatus.From(Degraded, Errors);

        Assert.Equal(StatusLight.Red, tile.Light);
        Assert.Equal("12 pending", tile.Text);
        Assert.Equal("Errors to review · Unavailable: app.log", tile.Detail);
    }

    [Fact]
    public void A_project_without_sources_is_neutral_and_says_how_to_fix_it()
    {
        var tile = TileStatus.From(new ProjectStatus(ProjectHealth.NotConfigured, []), Errors);

        Assert.Equal(StatusLight.Neutral, tile.Light);
        Assert.Equal("Not configured", tile.Text);
        Assert.Equal("Add a log source in its settings", tile.Detail);
    }

    [Fact]
    public void A_disabled_project_is_neutral_whatever_is_pending()
    {
        var tile = TileStatus.From(new ProjectStatus(ProjectHealth.Disabled, []), Errors);

        Assert.Equal(StatusLight.Neutral, tile.Light);
        Assert.Equal("Disabled", tile.Text);
        Assert.Equal("Monitoring is off", tile.Detail);
    }
}
