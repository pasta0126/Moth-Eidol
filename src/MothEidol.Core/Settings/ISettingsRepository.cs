namespace MothEidol.Core.Settings;

/// <summary>Key-value store for user settings. Missing keys return null.</summary>
public interface ISettingsRepository
{
    string? Get(string key);

    void Set(string key, string value);
}
