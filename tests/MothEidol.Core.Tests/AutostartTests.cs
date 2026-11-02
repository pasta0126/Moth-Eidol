using MothEidol.Core.Platform;

namespace MothEidol.Core.Tests;

public class AutostartTests
{
    [Fact]
    public void Unsupported_ReportsReasonAndIsDisabled()
    {
        IAutostart autostart = new UnsupportedAutostart("nope");

        Assert.False(autostart.IsSupported);
        Assert.Equal("nope", autostart.UnsupportedReason);
        Assert.False(autostart.IsEnabled);
        Assert.Throws<NotSupportedException>(() => autostart.SetEnabled(true));
    }

    [Fact]
    public void Factory_WithoutExecutablePath_IsUnsupported()
    {
        var autostart = AutostartFactory.Create(null);

        Assert.False(autostart.IsSupported);
    }

    private sealed class FakeGateway(StartupTaskState state, StartupTaskState afterRequest = StartupTaskState.Enabled)
        : IStartupTaskGateway
    {
        public StartupTaskState State { get; private set; } = state;

        public StartupTaskState GetState() => State;

        public StartupTaskState RequestEnable() => State = afterRequest;

        public void Disable() => State = StartupTaskState.Disabled;
    }

    [Fact]
    public void Packaged_EnableAndDisable_FollowTheTaskState()
    {
        var gateway = new FakeGateway(StartupTaskState.Disabled);
        IAutostart autostart = new PackagedAutostart(gateway);

        Assert.True(autostart.IsSupported);
        Assert.False(autostart.IsEnabled);
        autostart.SetEnabled(true);
        Assert.True(autostart.IsEnabled);
        autostart.SetEnabled(false);
        Assert.False(autostart.IsEnabled);
    }

    [Theory]
    [InlineData(StartupTaskState.DisabledByUser)]
    [InlineData(StartupTaskState.DisabledByPolicy)]
    public void Packaged_BlockedByOs_IsUnavailableWithReason(StartupTaskState state)
    {
        IAutostart autostart = new PackagedAutostart(new FakeGateway(state, afterRequest: state));

        Assert.False(autostart.IsSupported);
        Assert.False(string.IsNullOrEmpty(autostart.UnsupportedReason));
        Assert.False(autostart.IsEnabled);
        Assert.Throws<InvalidOperationException>(() => autostart.SetEnabled(true));
    }

    [Fact]
    public void Packaged_ReflectsUserTurningItOffInOsSettings()
    {
        var gateway = new FakeGateway(StartupTaskState.Enabled);
        IAutostart autostart = new PackagedAutostart(gateway);
        Assert.True(autostart.IsEnabled);

        gateway.Disable();
        Assert.False(autostart.IsEnabled);
    }

    [Fact]
    public void Factory_WhenNotPackaged_IgnoresTheGateway()
    {
        var autostart = AutostartFactory.Create("/some/app", new FakeGateway(StartupTaskState.Enabled));

        Assert.IsNotType<PackagedAutostart>(autostart);
    }
}
