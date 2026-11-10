using System.Reflection;

namespace MothEidol.Core;

/// <summary>Resolves the application's version (`x.y.z`, or `x.y.z.a` on development builds) from assembly metadata.</summary>
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
        if (version is null) return "0.0.0";
        var release = $"{version.Major}.{version.Minor}.{version.Build}";
        return version.Revision > 0 ? $"{release}.{version.Revision}" : release;
    }

    /// <summary>Strips build metadata (e.g. "+sha") leaving "x.y.z", "x.y.z.a" or "x.y.z-prerelease".</summary>
    public static string Normalize(string raw)
    {
        var plus = raw.IndexOf('+');
        return (plus >= 0 ? raw[..plus] : raw).Trim();
    }
}
