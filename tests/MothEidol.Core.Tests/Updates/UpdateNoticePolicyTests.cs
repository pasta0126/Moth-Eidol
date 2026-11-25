using MothEidol.Core.Updates;

namespace MothEidol.Core.Tests.Updates;

public sealed class UpdateNoticePolicyTests
{
    private static UpdateCheckResult Available(string version) => new UpdateCheckResult.Available(Version.Parse(version));

    [Theory]
    [InlineData("1.11.0", "1.12.0.0", "1.12.0")]   // Store versions carry a fourth part 0.
    [InlineData("1.11.0", "2.0.0.0", "2.0.0")]
    [InlineData("1.11.0.3", "1.11.1.0", "1.11.1")] // A dev build counts as its x.y.z.
    public void A_newer_published_version_is_announced(string running, string published, string expected)
    {
        var version = UpdateNoticePolicy.Decide(true, running, null, Available(published));

        Assert.Equal(Version.Parse(expected), version);
    }

    [Theory]
    [InlineData("1.12.0", "1.12.0.0")]
    [InlineData("1.12.0", "1.11.9.0")]
    [InlineData("1.12.0.2", "1.12.0.0")]
    public void The_same_or_an_older_version_is_not_announced(string running, string published)
    {
        Assert.Null(UpdateNoticePolicy.Decide(true, running, null, Available(published)));
    }

    [Fact]
    public void A_dismissed_version_is_not_announced()
    {
        Assert.Null(UpdateNoticePolicy.Decide(true, "1.11.0", "1.12.0", Available("1.12.0.0")));
    }

    [Fact]
    public void A_version_newer_than_the_dismissed_one_is_announced()
    {
        Assert.Equal(new Version(1, 13, 0), UpdateNoticePolicy.Decide(true, "1.11.0", "1.12.0", Available("1.13.0.0")));
    }

    [Fact]
    public void Nothing_is_announced_when_the_check_is_off()
    {
        Assert.Null(UpdateNoticePolicy.Decide(false, "1.11.0", null, Available("1.12.0.0")));
    }

    [Fact]
    public void Failures_and_no_update_announce_nothing()
    {
        Assert.Null(UpdateNoticePolicy.Decide(true, "1.11.0", null, new UpdateCheckResult.NoUpdate()));
        Assert.Null(UpdateNoticePolicy.Decide(true, "1.11.0", null, new UpdateCheckResult.Failed("offline")));
    }

    [Theory]
    [InlineData("")]
    [InlineData("garbage")]
    public void An_unreadable_dismissed_version_is_ignored(string dismissed)
    {
        Assert.Equal(new Version(1, 12, 0), UpdateNoticePolicy.Decide(true, "1.11.0", dismissed, Available("1.12.0.0")));
    }

    [Fact]
    public void An_unreadable_running_version_announces_nothing()
    {
        Assert.Null(UpdateNoticePolicy.Decide(true, "dev", null, Available("1.12.0.0")));
    }
}
