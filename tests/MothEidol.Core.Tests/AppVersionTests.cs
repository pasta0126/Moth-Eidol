using MothEidol.Core;

namespace MothEidol.Core.Tests;

public class AppVersionTests
{
    [Theory]
    [InlineData("0.1.0", "0.1.0")]
    [InlineData("0.1.0+abc123", "0.1.0")]
    [InlineData("1.2.3-beta.1+sha.5", "1.2.3-beta.1")]
    public void Normalize_StripsBuildMetadata(string raw, string expected)
    {
        Assert.Equal(expected, AppVersion.Normalize(raw));
    }

    [Fact]
    public void FromAssembly_ReturnsSemanticVersion()
    {
        var version = AppVersion.FromAssembly(typeof(AppVersion).Assembly);

        Assert.Matches(@"^\d+\.\d+\.\d+", version);
    }
}
