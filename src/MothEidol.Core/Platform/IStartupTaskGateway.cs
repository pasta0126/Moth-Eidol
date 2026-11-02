namespace MothEidol.Core.Platform;

/// <summary>State of the MSIX startup task, mirroring the OS values.</summary>
public enum StartupTaskState
{
    Disabled,
    DisabledByUser,
    DisabledByPolicy,
    Enabled,
    EnabledByPolicy,
}

/// <summary>Thin wrapper over the packaged-app startup task, so the logic stays testable without Windows.</summary>
public interface IStartupTaskGateway
{
    StartupTaskState GetState();

    /// <summary>Asks the OS to enable the task and returns the resulting state.</summary>
    StartupTaskState RequestEnable();

    void Disable();
}
