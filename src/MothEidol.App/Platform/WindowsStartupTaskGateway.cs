#if WINDOWS10_0_19041_0_OR_GREATER
using MothEidol.Core.Platform;
using Windows.ApplicationModel;

namespace MothEidol.App.Platform;

/// <summary>Real startup task gateway; only compiled for the Windows target framework.</summary>
internal sealed class WindowsStartupTaskGateway : IStartupTaskGateway
{
    private const string TaskId = "MothEidolStartup";

    public Core.Platform.StartupTaskState GetState() =>
        Map(Await(async () => (await StartupTask.GetAsync(TaskId)).State));

    public Core.Platform.StartupTaskState RequestEnable() =>
        Map(Await(async () => await (await StartupTask.GetAsync(TaskId)).RequestEnableAsync()));

    public void Disable() => Await(async () =>
    {
        (await StartupTask.GetAsync(TaskId)).Disable();
        return 0;
    });

    // Off the UI thread: the OS may show a prompt while enabling.
    private static T Await<T>(Func<Task<T>> work) => Task.Run(work).GetAwaiter().GetResult();

    private static Core.Platform.StartupTaskState Map(Windows.ApplicationModel.StartupTaskState state) => state switch
    {
        Windows.ApplicationModel.StartupTaskState.Enabled => Core.Platform.StartupTaskState.Enabled,
        Windows.ApplicationModel.StartupTaskState.EnabledByPolicy => Core.Platform.StartupTaskState.EnabledByPolicy,
        Windows.ApplicationModel.StartupTaskState.DisabledByUser => Core.Platform.StartupTaskState.DisabledByUser,
        Windows.ApplicationModel.StartupTaskState.DisabledByPolicy => Core.Platform.StartupTaskState.DisabledByPolicy,
        _ => Core.Platform.StartupTaskState.Disabled,
    };
}
#endif
