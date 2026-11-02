namespace MothEidol.Core.Platform;

/// <summary>
/// Start with session for an MSIX-packaged app, through the manifest's startup task. The user can also turn
/// it off from the OS startup apps settings; in that case the option is unavailable until re-enabled there.
/// </summary>
public sealed class PackagedAutostart(IStartupTaskGateway gateway) : IAutostart
{
    public bool IsSupported => gateway.GetState() is not (StartupTaskState.DisabledByUser or StartupTaskState.DisabledByPolicy);

    public string? UnsupportedReason => gateway.GetState() switch
    {
        StartupTaskState.DisabledByUser =>
            "Start with session was turned off in Windows Settings > Apps > Startup. Turn it back on there to use this option.",
        StartupTaskState.DisabledByPolicy => "Start with session is disabled by a system policy.",
        _ => null,
    };

    public bool IsEnabled => gateway.GetState() is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;

    public void SetEnabled(bool enabled)
    {
        if (enabled)
        {
            var state = gateway.RequestEnable();
            if (state is not (StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy))
            {
                throw new InvalidOperationException(UnsupportedReason ?? "Windows did not enable start with session.");
            }
        }
        else
        {
            gateway.Disable();
        }
    }
}
