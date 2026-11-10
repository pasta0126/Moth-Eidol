using MothEidol.Core.Settings;

namespace MothEidol.Core.Tests.Settings;

public sealed class ThemeSettingTests
{
    [Fact]
    public void A_new_installation_uses_the_moth_theme()
    {
        var settings = new AppSettings(new InMemorySettingsRepository());

        Assert.Equal(AppTheme.Moth, settings.Theme);
    }

    [Fact]
    public void The_chosen_theme_is_remembered()
    {
        var repository = new InMemorySettingsRepository();
        new AppSettings(repository).Theme = AppTheme.Vesper;

        Assert.Equal(AppTheme.Vesper, new AppSettings(repository).Theme);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Neon")]
    [InlineData("42")]
    [InlineData("vesper")]
    public void An_unknown_stored_value_falls_back_to_moth(string stored)
    {
        var repository = new InMemorySettingsRepository();
        repository.Set(AppSettings.Keys.Theme, stored);

        Assert.Equal(AppTheme.Moth, new AppSettings(repository).Theme);
    }

    [Fact]
    public void Going_back_to_moth_is_remembered_too()
    {
        var settings = new AppSettings(new InMemorySettingsRepository()) { Theme = AppTheme.Vesper };
        settings.Theme = AppTheme.Moth;

        Assert.Equal(AppTheme.Moth, settings.Theme);
    }
}
