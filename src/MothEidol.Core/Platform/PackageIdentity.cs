using System.Runtime.InteropServices;

namespace MothEidol.Core.Platform;

/// <summary>Detects whether the process runs with an MSIX package identity.</summary>
public static class PackageIdentity
{
    private const int AppModelErrorNoPackage = 15700;

    public static bool IsPackaged
    {
        get
        {
            if (!OperatingSystem.IsWindows())
            {
                return false;
            }

            try
            {
                var length = 0;
                return GetCurrentPackageFullName(ref length, null) != AppModelErrorNoPackage;
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, char[]? packageFullName);
}
