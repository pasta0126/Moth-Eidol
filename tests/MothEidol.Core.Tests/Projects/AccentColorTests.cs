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

    private const string Cream = "#FBF8F2";
    private const string Obsidian = "#302017";

    [Fact]
    public void A_legible_accent_is_left_as_it_is()
    {
        Assert.Equal("#202536", AccentColor.ReadableOn("#202536", Cream));
        Assert.Equal("#4FD1D9", AccentColor.ReadableOn("#4fd1d9", Obsidian));
    }

    [Theory]
    [InlineData("#E8E2D4")]
    [InlineData("#D9C79A")]
    [InlineData("#3FA7D6")]
    [InlineData("#4FD1D9")]
    public void A_pale_accent_is_darkened_to_be_readable_on_a_light_background(string accent)
    {
        var readable = AccentColor.ReadableOn(accent, Cream);

        Assert.True(AccentColor.ContrastRatio(readable, Cream) >= 4.5);
        Assert.NotEqual(accent, readable);
    }

    [Fact]
    public void A_dark_accent_is_lightened_to_be_readable_on_a_dark_background()
    {
        var readable = AccentColor.ReadableOn("#111111", Obsidian);

        Assert.True(AccentColor.ContrastRatio(readable, Obsidian) >= 4.5);
    }

    [Fact]
    public void Every_palette_color_is_readable_on_both_themes_after_the_shift()
    {
        foreach (var color in AccentColor.Palette)
        {
            Assert.True(AccentColor.ContrastRatio(AccentColor.ReadableOn(color, Cream), Cream) >= 4.5, color);
            Assert.True(AccentColor.ContrastRatio(AccentColor.ReadableOn(color, Obsidian), Obsidian) >= 4.5, color);
        }
    }

    [Fact]
    public void Contrast_matches_the_known_values()
    {
        Assert.Equal(21, AccentColor.ContrastRatio("#000000", "#FFFFFF"), precision: 1);
        Assert.Equal(1, AccentColor.ContrastRatio("#808080", "#808080"), precision: 3);
    }
}
