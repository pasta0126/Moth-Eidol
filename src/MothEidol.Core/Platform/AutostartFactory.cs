namespace MothEidol.Core.Platform;

public static class AutostartFactory
{
    public const string ValueName = "MothEidol";

    /// <param name="executablePath">Path of the running executable.</param>
    /// <param name="packagedGateway">Startup task gateway, used only when the app runs as an MSIX package.</param>
    public static IAutostart Create(string? executablePath, IStartupTaskGateway? packagedGateway = null)
    {
        if (packagedGateway is not null && PackageIdentity.IsPackaged)
        {
            return new PackagedAutostart(packagedGateway);
        }

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
