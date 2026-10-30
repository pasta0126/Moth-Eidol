using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace MothEidol.App.Views;

/// <summary>A small bar chart of counts (for example events per hour), drawn with the theme's brushes.</summary>
public sealed class SparklineControl : Control
{
    public static readonly StyledProperty<IReadOnlyList<int>?> ValuesProperty =
        AvaloniaProperty.Register<SparklineControl, IReadOnlyList<int>?>(nameof(Values));

    public static readonly StyledProperty<IBrush?> BrushProperty =
        AvaloniaProperty.Register<SparklineControl, IBrush?>(nameof(Brush));

    static SparklineControl()
    {
        AffectsRender<SparklineControl>(ValuesProperty, BrushProperty);
    }

    public IReadOnlyList<int>? Values
    {
        get => GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    public IBrush? Brush
    {
        get => GetValue(BrushProperty);
        set => SetValue(BrushProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var values = Values;
        if (values is not { Count: > 0 } || Brush is null || Bounds.Width <= 0 || Bounds.Height <= 0) return;

        var max = Math.Max(1, values.Max());
        var gap = values.Count > 40 ? 0.0 : 1.0;
        var barWidth = Math.Max(1.0, (Bounds.Width - gap * (values.Count - 1)) / values.Count);

        for (var i = 0; i < values.Count; i++)
        {
            var empty = values[i] == 0;
            var height = empty ? 1.0 : Math.Max(2.0, Bounds.Height * values[i] / max);
            var rect = new Rect(i * (barWidth + gap), Bounds.Height - height, barWidth, height);

            // Empty hours stay visible as a faint baseline so the time axis reads correctly.
            using (context.PushOpacity(empty ? 0.35 : 1.0))
            {
                context.DrawRectangle(Brush, null, rect);
            }
        }
    }
}
