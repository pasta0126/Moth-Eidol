using Avalonia;
using Avalonia.Media;

namespace MothEidol.App.Views;

/// <summary>Looks up the theme's <c>Moth*Brush</c> resources from code, so no color is hard-coded.</summary>
internal static class ThemeBrushes
{
    public static IBrush? Get(string key) =>
        Application.Current is { } app && app.TryGetResource(key, app.ActualThemeVariant, out var value)
            ? value as IBrush
            : null;
}
