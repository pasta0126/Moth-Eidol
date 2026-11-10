using MothEidol.Core.Settings;

namespace MothEidol.Core.Tests;

public sealed class SplashTests
{
    [Fact]
    public void The_splash_is_on_by_default()
    {
        Assert.True(new AppSettings(new InMemorySettingsRepository()).ShowSplash);
    }

    [Fact]
    public void Turning_the_splash_off_is_remembered()
    {
        var repository = new InMemorySettingsRepository();
        new AppSettings(repository).ShowSplash = false;

        Assert.False(new AppSettings(repository).ShowSplash);
    }

    [Fact]
    public void An_unreadable_stored_value_leaves_the_splash_on()
    {
        var repository = new InMemorySettingsRepository();
        repository.Set(AppSettings.Keys.ShowSplash, "maybe");

        Assert.True(new AppSettings(repository).ShowSplash);
    }

    [Theory]
    [InlineData(0, 2000)]
    [InlineData(300, 1700)]
    [InlineData(1999, 1)]
    [InlineData(2000, 0)]
    [InlineData(4000, 0)]
    public void The_splash_stays_until_it_has_been_visible_for_the_minimum(int visibleMs, int expectedMs)
    {
        Assert.Equal(TimeSpan.FromMilliseconds(expectedMs), SplashTiming.RemainingVisible(TimeSpan.FromMilliseconds(visibleMs)));
    }

    [Fact]
    public void The_minimum_is_two_seconds()
    {
        Assert.Equal(TimeSpan.FromSeconds(2), SplashTiming.MinimumVisible);
    }
}
