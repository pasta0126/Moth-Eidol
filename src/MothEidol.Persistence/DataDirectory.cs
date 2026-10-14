namespace MothEidol.Persistence;

public static class DataDirectory
{
    public const string AppFolder = "MothEidol";

    /// <summary>Per-user, local (non-roaming) data directory for the current platform.</summary>
    public static string Resolve()
    {
        if (OperatingSystem.IsMacOS())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, "Library", "Application Support", AppFolder);
        }

        // Windows: %LOCALAPPDATA%; Linux: $XDG_DATA_HOME or ~/.local/share.
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(local, AppFolder);
    }

    public static string DatabasePath(string directory) => Path.Combine(directory, "moth-eidol.db");
}
