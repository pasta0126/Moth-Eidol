using System.Globalization;

namespace MothEidol.Core.Settings;

/// <summary>Typed access to user settings with their default values.</summary>
public sealed class AppSettings(ISettingsRepository repository)
{
    public static class Keys
    {
        public const string StartWithSession = "app.start_with_session";
    }

    public bool StartWithSession
    {
        get => GetBool(Keys.StartWithSession, defaultValue: false);
        set => SetBool(Keys.StartWithSession, value);
    }

    private bool GetBool(string key, bool defaultValue) =>
        bool.TryParse(repository.Get(key), out var value) ? value : defaultValue;

    private void SetBool(string key, bool value) =>
        repository.Set(key, value.ToString(CultureInfo.InvariantCulture));
}
