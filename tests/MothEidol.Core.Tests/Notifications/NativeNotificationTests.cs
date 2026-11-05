using MothEidol.Core.Logs;
using MothEidol.Core.Notifications;
using MothEidol.Core.Projects;
using MothEidol.Core.Settings;

namespace MothEidol.Core.Tests.Notifications;

public class NativeNotificationTests
{
    [Fact]
    public void ToastXml_ShowsProjectLevelHeadlineAndDetail()
    {
        var xml = ToastXml.Build(new AlertText("Billing", EntryLevel.Error, "Failed to send", "in Mailer.Send"), silent: false);

        Assert.Contains("<text>Billing · Error</text>", xml);
        Assert.Contains("<text>Failed to send</text>", xml);
        Assert.Contains("<text>in Mailer.Send</text>", xml);
    }

    [Fact]
    public void ToastXml_WithSoundHasNoAudioElement()
    {
        var xml = ToastXml.Build(new AlertText("P", EntryLevel.Warning, "h", null), silent: false);
        Assert.DoesNotContain("audio", xml);
    }

    [Fact]
    public void ToastXml_SilentSwitchesTheSoundOff()
    {
        var xml = ToastXml.Build(new AlertText("P", EntryLevel.Warning, "h", null), silent: true);
        Assert.Contains("<audio silent=\"true\"", xml);
    }

    [Fact]
    public void ToastXml_OmitsAnEmptyDetail()
    {
        var xml = ToastXml.Build(new AlertText("P", EntryLevel.Fatal, "h", " "), silent: false);
        Assert.Equal(2, xml.Split("<text>").Length - 1);
    }

    [Fact]
    public void ToastXml_EscapesMarkupFromLogs()
    {
        var xml = ToastXml.Build(new AlertText("A&B", EntryLevel.Error, "<script>\"x\" & y</script>", "a < b"), silent: false);

        Assert.DoesNotContain("<script>", xml);
        Assert.Contains("A&amp;B", xml);
        Assert.Contains("&lt;script&gt;", xml);
        Assert.Contains("a &lt; b", xml);
        Assert.NotNull(System.Xml.Linq.XDocument.Parse(xml));
    }

    [Fact]
    public void Defaults_WindowsHasNativeAndSoundButNoToast()
    {
        Assert.True(NotificationDefaults.NativeExists(true));
        Assert.False(NotificationDefaults.ToastEnabledByDefault(true));
        Assert.True(NotificationDefaults.SoundEnabledByDefault(true));
    }

    [Fact]
    public void Defaults_OtherPlatformsKeepTheToastAndASilentStart()
    {
        Assert.False(NotificationDefaults.NativeExists(false));
        Assert.True(NotificationDefaults.ToastEnabledByDefault(false));
        Assert.False(NotificationDefaults.SoundEnabledByDefault(false));
    }

    [Fact]
    public void ToastNotifier_TakesItsDefaultFromTheConstructor()
    {
        var projects = new ProjectService(
            new InMemoryProjectRepository(),
            new TailingService(new InMemoryLogSourceRepository(), new InMemoryReadPositionStore()));

        Assert.False(new ToastNotifier(new ToastStack(), projects, enabledByDefault: false).EnabledByDefault);
        Assert.True(new ToastNotifier(new ToastStack(), projects).EnabledByDefault);
    }

    [Fact]
    public void StoredChoiceWinsOverThePlatformDefault()
    {
        var settings = new AppSettings(new InMemorySettingsRepository());

        Assert.False(settings.IsChannelEnabled(ChannelIds.Toast, NotificationDefaults.ToastEnabledByDefault(true)));
        settings.SetChannelEnabled(ChannelIds.Toast, true);
        Assert.True(settings.IsChannelEnabled(ChannelIds.Toast, NotificationDefaults.ToastEnabledByDefault(true)));

        Assert.True(settings.IsAlertSoundEnabled(NotificationDefaults.SoundEnabledByDefault(true)));
        settings.SetAlertSoundEnabled(false);
        Assert.False(settings.IsAlertSoundEnabled(NotificationDefaults.SoundEnabledByDefault(true)));
    }

    [Fact]
    public void AlertSound_PersistsAndSharesTheSoundChannelKey()
    {
        var repository = new InMemorySettingsRepository();
        new AppSettings(repository).SetAlertSoundEnabled(true);

        var reopened = new AppSettings(repository);
        Assert.True(reopened.IsAlertSoundEnabled(defaultValue: false));
        Assert.True(reopened.IsChannelEnabled(ChannelIds.Sound, defaultValue: false));
    }
}
