#if WINDOWS10_0_19041_0_OR_GREATER
using System.Runtime.Versioning;
using MothEidol.Core.Updates;
using Windows.ApplicationModel;
using Windows.Services.Store;

namespace MothEidol.App.Platform;

/// <summary>
/// Asks the Microsoft Store whether a newer version of the installed package is published. Only meaningful in the
/// Store (MSIX) build; nothing is downloaded or installed. An update whose version cannot be read is a failure, so
/// nothing unversioned is ever announced.
/// </summary>
[SupportedOSPlatform("windows10.0.19041.0")]
internal sealed class StoreUpdateSource : IUpdateSource
{
    public async Task<UpdateCheckResult> GetLatestAsync(CancellationToken cancellationToken)
    {
        try
        {
            var current = Package.Current.Id;
            var context = StoreContext.GetDefault();
            var updates = await context.GetAppAndOptionalStorePackageUpdatesAsync().AsTask(cancellationToken);
            if (updates.Count == 0) return new UpdateCheckResult.NoUpdate();

            // Updates of optional packages only do not concern the app itself.
            var own = updates
                .Where(u => string.Equals(u.Package?.Id.FamilyName, current.FamilyName, StringComparison.OrdinalIgnoreCase))
                .Select(u => ToVersion(u.Package.Id.Version))
                .ToList();
            if (own.Count == 0) return new UpdateCheckResult.NoUpdate();

            var newest = own.Max()!;
            return newest > ToVersion(current.Version)
                ? new UpdateCheckResult.Available(newest)
                : new UpdateCheckResult.Failed("The Store reports an update without a readable newer version.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new UpdateCheckResult.Failed($"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static Version ToVersion(PackageVersion v) => new(v.Major, v.Minor, v.Build, v.Revision);
}

/// <summary>Opens the application's own page in the Microsoft Store app.</summary>
[SupportedOSPlatform("windows10.0.19041.0")]
internal static class StorePage
{
    public static async Task<bool> OpenAsync()
    {
        try
        {
            var uri = new Uri($"ms-windows-store://pdp/?PFN={Uri.EscapeDataString(Package.Current.Id.FamilyName)}");
            return await Windows.System.Launcher.LaunchUriAsync(uri);
        }
        catch (Exception)
        {
            return false;
        }
    }
}
#endif
