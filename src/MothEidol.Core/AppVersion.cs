using System.Reflection;

namespace MothEidol.Core;

/// <summary>Resolves the application's semantic version from assembly metadata.</summary>
public static class AppVersion
{
    public static string FromAssembly(Assembly assembly)
    {
        var informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informational))
        {
            return Normalize(informational);
        }

        var version = assembly.GetName().Version;
        return version is null ? "0.0.0" : $"{version.Major}.{version.Minor}.{version.Build}";
    }

    /// <summary>Strips build metadata (e.g. "+sha") leaving "x.y.z" or "x.y.z-prerelease".</summary>
    public static string Normalize(string raw)
    {
        var plus = raw.IndexOf('+');
        return (plus >= 0 ? raw[..plus] : raw).Trim();
    }
}
