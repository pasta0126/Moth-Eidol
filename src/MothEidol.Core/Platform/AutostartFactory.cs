namespace MothEidol.Core.Platform;

public static class AutostartFactory
{
    public const string ValueName = "MothEidol";

    public static IAutostart Create(string? executablePath)
    {
        if (string.IsNullOrEmpty(executablePath))
        {
            return new UnsupportedAutostart("The application path could not be determined.");
        }

        if (OperatingSystem.IsWindows())
        {
            return new WindowsAutostart(ValueName, executablePath);
        }

        return new UnsupportedAutostart("Start with session is not available on this platform yet.");
    }
}
