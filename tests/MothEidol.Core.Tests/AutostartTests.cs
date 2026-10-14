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
}
