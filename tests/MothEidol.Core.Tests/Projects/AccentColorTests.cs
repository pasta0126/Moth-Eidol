using MothEidol.Core.Projects;

namespace MothEidol.Core.Tests.Projects;

public sealed class AccentColorTests
{
    [Theory]
    [InlineData("#3FA7D6", "#3FA7D6")]
    [InlineData("3fa7d6", "#3FA7D6")]
    [InlineData("  #3fa7d6 ", "#3FA7D6")]
    [InlineData("#abc", "#AABBCC")]
    [InlineData("F0a", "#FF00AA")]
    public void Valid_hex_is_normalized(string input, string expected) =>
        Assert.Equal(expected, AccentColor.Normalize(input));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("#")]
    [InlineData("#12ZZ99")]
    [InlineData("#1234")]
    [InlineData("#12345")]
    [InlineData("#1234567")]
    [InlineData("##123456")]
    [InlineData("red")]
    public void Invalid_hex_is_rejected(string? input) => Assert.Null(AccentColor.Normalize(input));

    [Fact]
    public void Palette_has_16_distinct_valid_colors()
    {
        Assert.Equal(16, AccentColor.Palette.Count);
        Assert.Equal(16, AccentColor.Palette.Distinct().Count());
        Assert.All(AccentColor.Palette, c => Assert.Equal(c, AccentColor.Normalize(c)));
    }
}
