using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;

namespace MothEidol.App.Views;

/// <summary>Looks up the theme's <c>Moth*Brush</c> resources from code, so no color is hard-coded.</summary>
internal static class ThemeBrushes
{
    /// <summary>
    /// Raised after the active theme changes. Brushes taken from code do not follow the theme by themselves, so
    /// long-lived views that keep them listen to this and refresh.
    /// </summary>
    public static event Action? Changed;

    public static void NotifyChanged() => Changed?.Invoke();

    /// <summary>A brush of the active theme.</summary>
    public static IBrush? Get(string key) => Get(key, variant: null);

    /// <summary>A brush of a given theme variant (Dark = Moth, Light = Vesper), whichever one is active.</summary>
    public static IBrush? Get(string key, ThemeVariant? variant) =>
        Application.Current is { } app && app.TryGetResource(key, variant ?? app.ActualThemeVariant, out var value)
            ? value as IBrush
            : null;
}
