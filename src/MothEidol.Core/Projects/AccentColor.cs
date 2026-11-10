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
}
