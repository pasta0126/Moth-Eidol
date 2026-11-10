namespace MothEidol.Core.Projects;

/// <summary>Parsing and the fixed palette of project accent colors.</summary>
public static class AccentColor
{
    /// <summary>
    /// The 16 colors offered in the project settings: legible on the dark background and distinct from the
    /// traffic-light hues used for status.
    /// </summary>
    public static IReadOnlyList<string> Palette { get; } =
    [
        "#3FA7D6", "#5B8DEF", "#7A6FF0", "#A66CFF",
        "#D45BD0", "#F06A9B", "#E58C8A", "#B7794A",
        "#2FB5A8", "#4FD1D9", "#7FD8B0", "#A9B86A",
        "#D9C79A", "#8C9BAB", "#B9A7E8", "#E8E2D4",
    ];

    /// <summary>
    /// Normalizes <c>#RRGGBB</c>, <c>RRGGBB</c>, <c>#RGB</c> or <c>RGB</c> (any case) to <c>#RRGGBB</c> in upper
    /// case, or returns null when the text is not a hex color.
    /// </summary>
    public static string? Normalize(string? text)
    {
        var hex = text?.Trim() ?? "";
        if (hex.StartsWith('#')) hex = hex[1..];
        if (hex.Length is not (3 or 6) || !hex.All(Uri.IsHexDigit)) return null;
        if (hex.Length == 3) hex = string.Concat(hex.Select(c => new string(c, 2)));
        return "#" + hex.ToUpperInvariant();
    }

    /// <summary>
    /// The accent shifted toward the text color (white on a dark background, black on a light one) only as far
    /// as needed to reach <paramref name="minContrast"/> against the background, so it stays readable as text.
    /// A color that already has enough contrast is returned unchanged (normalized). Both colors are <c>#RRGGBB</c>.
    /// </summary>
    public static string ReadableOn(string accent, string background, double minContrast = 4.5)
    {
        var color = Normalize(accent) ?? throw new ArgumentException("Not a hex color.", nameof(accent));
        var back = Normalize(background) ?? throw new ArgumentException("Not a hex color.", nameof(background));

        var (br, bg, bb) = Rgb(back);
        var target = Luminance(br, bg, bb) < 0.18 ? 255 : 0;
        var (r, g, b) = Rgb(color);
        for (var step = 0; step <= 20; step++)
        {
            var t = step / 20.0;
            var (mr, mg, mb) = (Mix(r, target, t), Mix(g, target, t), Mix(b, target, t));
            if (Contrast(Luminance(mr, mg, mb), Luminance(br, bg, bb)) >= minContrast)
                return $"#{mr:X2}{mg:X2}{mb:X2}";
        }
        return $"#{target:X2}{target:X2}{target:X2}";
    }

    /// <summary>WCAG contrast ratio between two <c>#RRGGBB</c> colors.</summary>
    public static double ContrastRatio(string first, string second)
    {
        var (ar, ag, ab) = Rgb(Normalize(first) ?? throw new ArgumentException("Not a hex color.", nameof(first)));
        var (cr, cg, cb) = Rgb(Normalize(second) ?? throw new ArgumentException("Not a hex color.", nameof(second)));
        return Contrast(Luminance(ar, ag, ab), Luminance(cr, cg, cb));
    }

    private static (int R, int G, int B) Rgb(string hex) =>
        (Convert.ToInt32(hex[1..3], 16), Convert.ToInt32(hex[3..5], 16), Convert.ToInt32(hex[5..7], 16));

    private static int Mix(int from, int to, double t) => (int)Math.Round(from + (to - from) * t);

    private static double Contrast(double a, double b) => (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);

    private static double Luminance(int r, int g, int b)
    {
        static double Channel(int value)
        {
            var c = value / 255.0;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }
        return 0.2126 * Channel(r) + 0.7152 * Channel(g) + 0.0722 * Channel(b);
    }
}
