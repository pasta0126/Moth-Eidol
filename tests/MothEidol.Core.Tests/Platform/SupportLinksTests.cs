using MothEidol.Core.Platform;
using Xunit;

namespace MothEidol.Core.Tests.Platform;

public class SupportLinksTests
{
    [Theory]
    [InlineData(SupportLinks.BuyMeACoffee)]
    [InlineData(SupportLinks.SuggestionsAndBugs)]
    [InlineData(SupportLinks.GitHub)]
    public void Fixed_https_addresses_are_allowed(string url) => Assert.True(SupportLinks.IsAllowed(url));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("http://buymeacoffee.com/pasta0126")]
    [InlineData("https://example.com/")]
    [InlineData("https://buymeacoffee.com/pasta0126/extra")]
    [InlineData("file:///etc/passwd")]
    [InlineData("javascript:alert(1)")]
    [InlineData("calc.exe")]
    public void Anything_else_is_rejected(string? url) => Assert.False(SupportLinks.IsAllowed(url));

    [Fact]
    public void The_opener_refuses_a_disallowed_address_without_starting_anything()
    {
        var ok = new SystemUrlOpener().TryOpen("https://example.com/", out var error);

        Assert.False(ok);
        Assert.False(string.IsNullOrEmpty(error));
    }
}
