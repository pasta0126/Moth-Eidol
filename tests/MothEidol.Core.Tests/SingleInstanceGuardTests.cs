using MothEidol.Core;

namespace MothEidol.Core.Tests;

public class SingleInstanceGuardTests
{
    [Fact]
    public async Task SecondInstance_IsRejected_AndSignalsPrimary()
    {
        var name = $"MothEidol.Tests.{Guid.NewGuid():N}";
        using var primary = new SingleInstanceGuard(name);
        var activated = new TaskCompletionSource();
        primary.ActivationRequested += () => activated.TrySetResult();

        Assert.True(primary.TryAcquire());

        using var secondary = new SingleInstanceGuard(name);
        Assert.False(secondary.TryAcquire());

        var completed = await Task.WhenAny(activated.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.Same(activated.Task, completed);
    }
}
